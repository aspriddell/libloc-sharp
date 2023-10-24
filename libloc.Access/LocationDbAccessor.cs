// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DragonFruit.Data;
using libloc.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SharpCompress.Compressors.Xz;

namespace libloc.Access
{
    internal class LocationDbAccessor : ILocationDbAccessor, IHostedService, IDisposable
    {
        private readonly ILogger<LocationDbAccessor> _logger;
        private readonly IConfiguration _configuration;
        private readonly AsyncReaderWriterLock _lock;
        private readonly IDisposable _initialLock;
        private readonly ApiClient _client;

        private Timer _fetchTimer;
        private ILocationDatabase _database;
        private DateTimeOffset? _databaseCreationTime;

        private const string DatabaseName = "location-{0}.db";
        private static readonly Regex DatabaseVersionMatcher = new(@"location-(\d+)\.db", RegexOptions.Compiled);

        public LocationDbAccessor(ILogger<LocationDbAccessor> logger, IConfiguration configuration, ApiClient client)
        {
            _logger = logger;
            _client = client;
            _configuration = configuration;
            _lock = new AsyncReaderWriterLock();

            _initialLock = _lock.WriterLock();
        }

        private string DatabaseStorageRoot => _configuration["LocationDb:StorageRoot"] ?? Path.GetFullPath(".");

        public async Task PerformAsync(Func<ILocationDatabase, Task> action)
        {
            using (await _lock.ReaderLockAsync().ConfigureAwait(false))
            {
                await action.Invoke(_database);
            }
        }

        public async Task<T> PerformAsync<T>(Func<ILocationDatabase, T> action)
        {
            using (await _lock.ReaderLockAsync().ConfigureAwait(false))
            {
                return action.Invoke(_database);
            }
        }

        public async Task<T> PerformAsync<T>(Func<ILocationDatabase, Task<T>> action)
        {
            using (await _lock.ReaderLockAsync().ConfigureAwait(false))
            {
                return await action.Invoke(_database);
            }
        }

        private async Task<bool> DownloadLatestDatabase()
        {
            var downloadRequest = new LocationDbDownloadRequest
            {
                LastDownload = _databaseCreationTime ?? _database?.CreatedAt
            };

            _logger.LogDebug("Performing location.db download request using {x} expiry date", downloadRequest.LastDownload?.ToString("r") ?? "no");
            using var response = await _client.PerformAsync(downloadRequest).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    var version = (response.Content.Headers.LastModified ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
                    var databaseFileLocation = Path.Combine(DatabaseStorageRoot, string.Format(DatabaseName, version));

                    if (File.Exists(databaseFileLocation))
                    {
                        _logger.LogInformation("File {name} already exists, skipping...", Path.GetFileName(databaseFileLocation));
                        return false;
                    }

                    _logger.LogInformation("New location.db discovered, writing version {ver} to disk", version);

                    try
                    {
                        // download to temp file, expand and copy to new file before switching old database out.
                        await using var tempStream = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

                        var stopwatch = new Stopwatch();
                        await using (var content = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            stopwatch.Start();
                            await content.CopyToAsync(tempStream).ConfigureAwait(false);
                        }

                        stopwatch.Stop();
                        tempStream.Seek(0, SeekOrigin.Begin);

                        _logger.LogInformation("location.db written to temp file successfully (in {x:0.00} seconds)", stopwatch.Elapsed.TotalSeconds);

                        // create an XZStream over the temp stream and write to the destination
                        await using (var xzStream = new XZStream(tempStream))
                        await using (var destinationStream = new FileStream(databaseFileLocation, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                        {
                            stopwatch.Restart();

                            await xzStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                            await destinationStream.FlushAsync().ConfigureAwait(false);
                        }

                        stopwatch.Stop();
                        _logger.LogInformation("location.db written to disk (location {dir}) in {x} seconds", databaseFileLocation, stopwatch.Elapsed.TotalSeconds.ToString("0.###"));
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("location.db download failed - {message}", e.Message);
                        return false;
                    }

                    return true;
                }

                case HttpStatusCode.NotModified:
                    _logger.LogInformation("location.db not modified since {date}, check completed successfully", response.Content.Headers.LastModified);
                    return false;

                default:
                    _logger.LogWarning("location.db check returned an unexpected result - {code}", response.StatusCode);
                    _logger.LogDebug("location.db check response - {content}", await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    return false;
            }
        }

        private async ValueTask<bool> LoadLatestDatabase(bool enforceLock = true)
        {
            // load in latest database from directory
            var dbFiles = Directory.GetFiles(DatabaseStorageRoot, string.Format(DatabaseName, "*"), SearchOption.TopDirectoryOnly);

            using (enforceLock ? await _lock.WriterLockAsync().ConfigureAwait(false) : null)
            {
                foreach (var dbFile in dbFiles.OrderByDescending(File.GetLastWriteTimeUtc))
                {
                    var fileName = Path.GetFileName(dbFile);

                    try
                    {
                        _logger.LogInformation("Loading db {file}", fileName);
                        var newDatabase = DatabaseLoader.LoadFromFile(dbFile);

                        _database?.Dispose();
                        _database = newDatabase;

                        // use regex to get more accurate version info
                        var nameMatch = DatabaseVersionMatcher.Match(fileName);
                        _databaseCreationTime = nameMatch.Success ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(nameMatch.Groups[1].Value)) : null;

                        _logger.LogInformation("Successfully loaded db {file} with expiry {exp}", fileName, _databaseCreationTime);

                        // remove all other location.db files
                        foreach (var file in dbFiles.Where(x => x != dbFile))
                        {
                            _logger.LogDebug("Removing old location db {name}", Path.GetFileName(file));
                            File.Delete(file);
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("{db} failed to load - {message}", fileName, ex.Message);
                    }
                }
            }

            return false;
        }

        private async Task PerformUpdate()
        {
            _logger.LogInformation("Performing location database update");

            if (await DownloadLatestDatabase().ConfigureAwait(false))
            {
                await LoadLatestDatabase().ConfigureAwait(false);
            }
        }

        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            using (_initialLock)
            {
                // load from disk
                var dbLoaded = await LoadLatestDatabase(false).ConfigureAwait(false);
                var initialTimeout = dbLoaded ? TimeSpan.Zero : TimeSpan.FromHours(24);

                while (!dbLoaded)
                {
                    // download new database from network
                    if (!await DownloadLatestDatabase().ConfigureAwait(false))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        dbLoaded = await LoadLatestDatabase(false).ConfigureAwait(false);
                    }
                }

                _fetchTimer = new Timer(_ => PerformUpdate(), null, initialTimeout, TimeSpan.FromHours(24));
            }
        }

        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            await _fetchTimer.DisposeAsync();
        }

        public void Dispose()
        {
            _database?.Dispose();
            _initialLock?.Dispose();
        }
    }
}
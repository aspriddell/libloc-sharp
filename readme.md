# libloc-sharp
A version of IPFire's libloc library rewritten for C#

## Overview
This is a rewritten version of libloc (an IP address database used by IPFire firewall software to screen network traffic)
along with an additional library, `libloc.Access`, that enables the database to be loaded as a hosted service, enabling automatic database updates and access control.

The core libloc project has been written to require a single 3rd party library (IPNetwork) and has most of the expected read functionality.

### Usage (libloc)
The libloc `DatabaseLoader.LoadFromFile(string path)` returns an `ILocationDatabase` if the database was successfully loaded in. Accessing different features of the database can then be done through this instance:

- `ILocationDatabase.AS` provides a database of Autonomous Systems, where the entire collection can be accessed, or a specific entry can be loaded by ASN
- `ILocationDatabase.Networks` provides all networks that own a block of IP addresses which can be accessed by index or enumerated over
- `ILocationDatabase.Countries` provides a list of all countries and their continents. These can be accessed by country code, index or enumerated over
- `ILocationDatabase.ResolveAddress(IPAddress address)` performs a network tree traversal to locate the network a specific address belongs to, along with the ASN of the owner, the start and end addresses and the prefix length.
These values are stored and processed as IPv6, so an IPv4 address will need mapping back from v6 if desired.
- `ILocationDatabase.GetEnumerator(AddressFamily family)` provides an enumerator that can be used to get all networks, and their corresponding address blocks.

```csharp
var database = DatabaseLoader.LoadFromFile("C:\location.db");

// locate 1.1.1.1
var networkInfo = database.ResolveAddress(IPAddress.Parse("1.1.1.1"));

// load AS info
var asInfo = database.AS[networkInfo.ASN];
```

### Usage (libloc.Access)

There are a few things to note when using `libloc.Access`:

- A `DragonFruit.Data.ApiClient` must be registered in the dependency container - see [here](https://github.com/dragonfruitnetwork/dragonfruit-common/wiki/%5BApiClient%5D-Getting-Started) for more information
- `IConfiguration` is used to load service preferences in (under the `LocationDb` section)
- `ILogger` is injected into the database accessor for logging and diagnosing the database update process

To use the database accessor, use `builder.Services.AddLocationDb()` in the startup code (after the client has been registered) and access the database using the `ILocationDbAccessor` interface:

```csharp
builder.Serivces.AddSingleton<ApiClient<ApiSystemTextJsonSerializer>>();

// add databases
builder.Services.AddLocationDb();
builder.Services.AddDbContext<DummyDatabase>(c => c.UseNpgsql("Host=localhost"));

// after building the host, you can access the database via dependency injection
var locationDb = services.GetRequiredService<ILocationDbAccessor>();
var network = await locationDb.PerformAsync(db => db.ResolveNetwork(IPAddress.Parse("1.1.1.1")));
```

### License
The project is licensed under LGPL-2.1, the same as the original library.
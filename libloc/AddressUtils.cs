// liblocsharp - A version of IPFire's libloc library rewritten for C#
// Licensed under LGPL-2.1 - see the license file for more information

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace libloc
{
    internal static class AddressUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetAddressBit(Span<byte> addressBytes, int depth)
        {
            return (addressBytes[depth / 8] >> (7 - depth % 8)) & 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAddressBit(Span<byte> addressBytes, int depth, int value)
        {
            addressBytes[depth / 8] ^= (byte)((-value ^ addressBytes[depth / 8]) & (1 << (7 - depth % 8)));
        }

        public static AddressFamily GetAddressFamily(ReadOnlySpan<byte> addressBytes)
        {
            for (var i = 0; i < 10; i++)
            {
                if (addressBytes[i] > 0)
                {
                    return AddressFamily.InterNetworkV6;
                }
            }

            for (var i = 10; i < 12; i++)
            {
                if (addressBytes[i] != 0xff)
                {
                    return AddressFamily.InterNetworkV6;
                }
            }

            return AddressFamily.InterNetwork;
        }
    }
}
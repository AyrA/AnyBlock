using System;
using System.Net;

namespace WinAPI.NET
{
    public class CIDR
    {
        /// <summary>
        /// Gets the IP Address
        /// </summary>
        public IPAddress Address
        { get; private set; }

        /// <summary>
        /// Gets the lowest IP Address of this range
        /// </summary>
        public IPAddress AddressLow
        { get; private set; }

        /// <summary>
        /// Gets the highest IP Address of this range
        /// </summary>
        public IPAddress AddressHigh
        { get; private set; }

        /// <summary>
        /// Gets the Network Mask
        /// </summary>
        public int Mask
        { get; private set; }

        /// <summary>
        /// Initializes a new Range in CIDR Notation
        /// </summary>
        /// <param name="CombinedNotation">IP/Mask</param>
        public CIDR(string CombinedNotation)
        {
            IPAddress TempAddr;
            int TempMask = 0;
            if (string.IsNullOrWhiteSpace(CombinedNotation))
            {
                throw new ArgumentNullException(nameof(CombinedNotation));
            }
            var Parts = CombinedNotation.Split('/');
            if (Parts.Length > 2)
            {
                throw new FormatException("CombinedNotation is in an invalid Format");
            }
            if (!IPAddress.TryParse(Parts[0], out TempAddr))
            {
                throw new FormatException("CombinedNotation doesn't has a valid IP Address");
            }
            if (Parts.Length == 1)
            {
                Mask = TempAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32;
            }
            else
            {
                if (!int.TryParse(Parts[1], out TempMask))
                {
                    throw new FormatException("CombinedNotation doesn't has a valid CIDR Mask");
                }
                if (TempMask < 0)
                {
                    throw new FormatException("CIDR Mask is outside of Bounds");
                }
                if (TempMask > (TempAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? 128 : 32))
                {
                    throw new FormatException("CIDR Mask is outside of Bounds");
                }
            }
            Mask = TempMask;
            Address = TempAddr;
        }

        /// <summary>
        /// Gets this object as CIDR notation
        /// </summary>
        /// <returns>IP/Mask</returns>
        public override string ToString()
        {
            return $"{Address}/{Mask}";
        }
    }
}

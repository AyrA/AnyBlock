using System;
using System.Net;
using System.Net.Sockets;

namespace WinAPI.NET
{
    /// <summary>
    /// IP version specifier
    /// </summary>
    [Flags]
    public enum IPVersion : int
    {
        /// <summary>
        /// No or invalid value
        /// </summary>
        None = 0,
        /// <summary>
        /// IPv4
        /// </summary>
        V4 = 1,
        /// <summary>
        /// IPv6
        /// </summary>
        V6 = 2,
        /// <summary>
        /// IPv4 or IPv6
        /// </summary>
        Any = V4 | V6
    }

    /// <summary>
    /// Handles CIDR notation of IPv4 and IPv6 addresses
    /// </summary>
    public class CIDR : ICloneable
    {
        /// <summary>
        /// Gets the IP address supplied in the constructor
        /// </summary>
        public IPAddress Address
        { get; private set; }

        /// <summary>
        /// Gets the lowest IP Address of this CIDR range
        /// </summary>
        public IPAddress AddressLow
        { get; private set; }

        /// <summary>
        /// Gets the highest IP address of this range
        /// </summary>
        public IPAddress AddressHigh
        { get; private set; }

        /// <summary>
        /// Gets the network mask as the number of enabled bits
        /// </summary>
        public int Mask
        { get; private set; }

        /// <summary>
        /// Gets the network mask as a byte array
        /// </summary>
        public byte[] MaskBits
        { get; private set; }

        /// <summary>
        /// Gets the current bitmask as traditional IP notation
        /// </summary>
        /// <remarks>
        /// Will work for IPv6 but doesn't makes too much sense.
        /// The scope ID is not kept for the v6 mask.
        /// </remarks>
        public IPAddress MaskIP
        {
            get
            {
                return new IPAddress(MaskBits);
            }
        }

        /// <summary>
        /// Gets or sets whether the CIDR notation of <see cref="ToString"/> should be fixed
        /// to use the lowest address if necessary.
        /// </summary>
        public bool FixAddress
        { get; set; }

        /// <summary>
        /// Gets the Type of address in use
        /// </summary>
        public IPVersion Type
        {
            get
            {
                return Address.AddressFamily == AddressFamily.InterNetwork ? IPVersion.V4 : IPVersion.V6;
            }
        }

        /// <summary>
        /// Initializes a new Range in CIDR Notation
        /// </summary>
        /// <param name="combinedNotation">IP/Mask</param>
        /// <param name="fixAddress">Fixes the CIDR notation of <see cref="ToString"/> if necessary</param>
        public CIDR(string combinedNotation, bool fixAddress)
        {
            IPAddress tempAddr;
            FixAddress = fixAddress;
            int tempMask = 0;
            //Require first parameter
            if (string.IsNullOrWhiteSpace(combinedNotation))
            {
                throw new ArgumentNullException(nameof(combinedNotation));
            }
            var Parts = combinedNotation.Split('/');
            //Require at most one CIDR delimiter
            if (Parts.Length > 2)
            {
                throw new FormatException("CombinedNotation is in an invalid Format");
            }
            //Require a valid IP address
            if (!IPAddress.TryParse(Parts[0], out tempAddr))
            {
                throw new FormatException("CombinedNotation doesn't has a valid IP Address");
            }
            if (tempAddr.AddressFamily != AddressFamily.InterNetwork && tempAddr.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new FormatException("Supplied address is not IPv4 or IPv6");
            }
            //If no CIDR delimiter is provided, assume fully closed mask
            if (Parts.Length == 1)
            {
                tempMask = tempAddr.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            }
            else
            {
                //Mask must be integer
                if (!int.TryParse(Parts[1], out tempMask))
                {
                    throw new FormatException("CombinedNotation doesn't has a valid CIDR Mask");
                }
                //Mask must be 0 or bigger
                if (tempMask < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(combinedNotation), "CIDR Mask is outside of Bounds");
                }
                //Mask must not be bigger than IPv4=32 or IPv6=128
                if (tempMask > (tempAddr.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32))
                {
                    throw new ArgumentOutOfRangeException(nameof(combinedNotation), "CIDR Mask is outside of Bounds");
                }
            }
            ComputeMask(tempAddr, tempMask);
        }

        /// <summary>
        /// Changes the CIDR Mask to another
        /// </summary>
        /// <param name="cidrMask">CIDR Mask as number of enabled bits</param>
        public void SetMask(int cidrMask)
        {
            //Mask must be 0 or bigger
            if (cidrMask < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cidrMask), "CIDR Mask is outside of Bounds");
            }
            //Mask must not be bigger than IPv4=32 or IPv6=128
            if (cidrMask > (Address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32))
            {
                throw new ArgumentOutOfRangeException(nameof(cidrMask), "CIDR Mask is outside of Bounds");
            }
            ComputeMask(Address, cidrMask);
        }

        /// <summary>
        /// Changes the CIDR Mask to another
        /// </summary>
        /// <param name="cidrMask">CIDR Mask as byte array</param>
        public void SetMask(byte[] cidrMask)
        {
            int mask = 0;
            var zero = false;
            if (cidrMask == null)
            {
                throw new ArgumentNullException(nameof(cidrMask));
            }
            foreach (var b in cidrMask)
            {
                switch (b)
                {
                    case 0xFE:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 7;
                        zero = true;
                        break;
                    case 0xFC:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 6;
                        zero = true;
                        break;
                    case 0xF8:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 5;
                        zero = true;
                        break;
                    case 0xF0:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 4;
                        zero = true;
                        break;
                    case 0xE0:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 3;
                        zero = true;
                        break;
                    case 0xC0:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 2;
                        zero = true;
                        break;
                    case 0x80:
                        if (zero) throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                        mask += 1;
                        zero = true;
                        break;
                    case 0x00:
                        break;
                    default:
                        throw new ArgumentException($"Byte value {b} is not a valid value for a mask", nameof(cidrMask));
                }
            }
            SetMask(mask);
        }

        /// <summary>
        /// Changes the CIDR Mask to another
        /// </summary>
        /// <param name="cidrMask">CIDR Mask as IP address</param>
        public void SetMask(IPAddress cidrMask)
        {
            try
            {
                SetMask(cidrMask.GetAddressBytes());
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{cidrMask} is an invalid Bitmask", ex);
            }
        }

        /// <summary>
        /// Computes various CIDR related properties of this instance
        /// </summary>
        /// <param name="ipAddr">Any IP address</param>
        /// <param name="cidrMask">Any valid CIDR mask</param>
        private void ComputeMask(IPAddress ipAddr, int cidrMask)
        {
            //Mask to work on
            var mask = cidrMask;
            //Original IP bytes
            var bytes = ipAddr.GetAddressBytes();
            //Lowest IP bytes
            var low = (byte[])bytes.Clone();
            //Highest IP bytes
            var high = (byte[])bytes.Clone();
            //Bitmask
            var bitmask = new byte[low.Length];

            //Create and apply bitmask
            //Note: Binary operators are not defined for the "byte" type for some reason.
            for (var i = 0; i < bitmask.Length; i++)
            {
                //Get the current section of the mask in the range of 0-8
                var Current = Math.Min(8, Math.Max(0, mask));
                //Apply mask to bitmask array
                bitmask[i] = (byte)(0xFF << (8 - Current) & 0xFF);
                //Remove current section of the mask
                mask -= 8;

                //Lowest address is simply "IP & bitmask"
                //IP:   10101010
                //Mask: 11110000
                //New:  10100000
                low[i] &= bitmask[i];

                //Highest address sets those bits to 1 that are 0 in the bitmask and leaves the other bits unchanged.
                //IP:   10101010
                //Mask: 11110000 (InvMask is just ~Mask==00001111)
                //New:  10101111 (Because IP|InvMask)
                high[i] |= (byte)(~bitmask[i]);
            }

            //Set bitmask related properties
            MaskBits = bitmask;
            Mask = cidrMask;
            //Set addresses and keep scope if applicable
            if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
            {
                Address = new IPAddress(bytes);
                AddressLow = new IPAddress(low);
                AddressHigh = new IPAddress(high);
            }
            else
            {
                Address = new IPAddress(bytes, ipAddr.ScopeId);
                AddressLow = new IPAddress(low, ipAddr.ScopeId);
                AddressHigh = new IPAddress(high, ipAddr.ScopeId);
            }
        }

        /// <summary>
        /// Checks if the given address is inside the current CIDR range
        /// </summary>
        /// <param name="Addr">IP address</param>
        /// <returns><see cref="true"/>, if inside the current CIDR range</returns>
        /// <remarks><paramref name="Addr"/> must have the same <see cref="IPAddress.AddressFamily"/> as the current CIDR range</remarks>
        public bool IsInside(IPAddress Addr)
        {
            if (Addr.AddressFamily != Address.AddressFamily)
            {
                throw new ArgumentException($"Supplied Address is {Addr.AddressFamily} but CIDR range is {Address.AddressFamily}");
            }
            var bytes = Addr.GetAddressBytes();
            var low = AddressLow.GetAddressBytes();

            //Apply the bitmask to the supplied address and abort
            //if it's not the same as the low address of this CIDR range
            for (var i = 0; i < MaskBits.Length; i++)
            {
                if ((bytes[i] & MaskBits[i]) != low[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets this object as CIDR notation
        /// </summary>
        /// <returns>IP/Mask</returns>
        public override string ToString()
        {
            return $"{(FixAddress ? AddressLow : Address)}/{Mask}";
        }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns>Cloned copy</returns>
        public object Clone()
        {
            return new CIDR(Address.ToString() + "/" + Mask.ToString(), FixAddress);
        }
    }
}

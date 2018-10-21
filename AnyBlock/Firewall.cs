using AnyBlock;
using NetFwTypeLib;
using System;
using System.Linq;

namespace WinAPI
{
    public static class Firewall
    {
        /// <summary>
        /// Number of Ranges supported in a single Rule
        /// </summary>
        public const int BLOCKSIZE = 1000;

        /// <summary>
        /// Rule Name Prefix
        /// </summary>
        public const string BLOCK = "AnyBlock";

        /// <summary>
        /// Gets the Firewall Policy Object
        /// </summary>
        /// <returns></returns>
        private static INetFwPolicy2 GetPolicy()
        {
            return (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        }

        /// <summary>
        /// Removes all Rules
        /// </summary>
        public static void ClearRules()
        {
            var Policy = GetPolicy();
            var Rules = Policy.Rules
                .Cast<INetFwRule2>()
                .Where(m => m.Name.StartsWith($"{BLOCK}."))
                .Select(m => m.Name)
                .ToArray();
            foreach (var Rule in Rules)
            {
                Console.Error.Write('.');
                Policy.Rules.Remove(Rule);
            }
            Console.Error.WriteLine();
        }

        /// <summary>
        /// Blocks A Country
        /// </summary>
        /// <param name="CountryCode">Country Code</param>
        /// <param name="IPList">IP Address List to Block</param>
        /// <param name="D">Direction to Block</param>
        /// <remarks>This first completely unblocks said Country</remarks>
        public static void BlockRanges(RangeEntry[] Ranges)
        {
            var IN = Ranges.Where(m => m.Direction.HasFlag(Direction.IN)).SelectMany(m => Cache.GetAddresses(m.Name)).ToArray();
            var OUT = Ranges.Where(m => m.Direction.HasFlag(Direction.OUT)).SelectMany(m => Cache.GetAddresses(m.Name)).ToArray();
            if (IN.Length > 0)
            {
                IpBlock(IN, Direction.IN);
            }
            if (OUT.Length > 0)
            {
                IpBlock(OUT, Direction.OUT);
            }
        }

        /// <summary>
        /// Blocks an IP List into the given Direction
        /// </summary>
        /// <param name="IPList">IP List</param>
        /// <param name="D">Direction</param>
        private static void IpBlock(string[] IPList, Direction D)
        {
            if (D == Direction.DISABLED)
            {
                return;
            }
            if (D == Direction.BOTH)
            {
                IpBlock(IPList, Direction.IN);
                IpBlock(IPList, Direction.OUT);
            }
            else
            {
                var Policy = GetPolicy();
                for (var IpCount = 0; IpCount < IPList.Length; IpCount += BLOCKSIZE)
                {
                    Console.Error.Write('.');
                    var R = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule"));

                    R.Name = $"{BLOCK}.{D}.{(IpCount + BLOCKSIZE) / BLOCKSIZE}";
                    R.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                    R.Description = $"AnyBlock Rule";
                    R.Direction = D == Direction.IN ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    R.Enabled = true;
                    R.InterfaceTypes = "All";
                    R.LocalAddresses = "*";
                    R.Profiles = int.MaxValue;
                    R.Protocol = 256; //Any
                    R.RemoteAddresses = string.Join(",", IPList.Skip(IpCount).Take(BLOCKSIZE).ToArray());
                    Policy.Rules.Add(R);
                }
                Console.Error.WriteLine();
            }
        }
    }
}

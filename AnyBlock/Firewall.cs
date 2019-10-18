using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WinAPI.NET
{
    /// <summary>
    /// Provides easy handling of firewall rules
    /// </summary>
    public static class Firewall
    {
        /// <summary>
        /// Number of entries supported in a single rule
        /// </summary>
        /// <remarks>
        /// 1000 is the current maximum for Windows firewall. Do not increase further.
        /// The firewall limits the number of entries and not the number of addresses.
        /// </remarks>
        public const int BLOCKSIZE = 1000;

        /// <summary>
        /// Rule name prefix
        /// </summary>
        /// <remarks>
        /// Rule adding and removal is based on the assembly name by default.
        /// Either chose a distinct but meaningful assembly name or change this manually at runtime.
        /// </remarks>
        public static string RulePrefix;

        /// <summary>
        /// Separator to use in JSON names
        /// </summary>
        /// <remarks>
        /// The dot it no longer appropriate since this tool started supporting ASN ranges.
        /// Make sure no name contains this separator
        /// </remarks>
        public const char SEPARATOR = '|';

        /// <summary>
        /// Static initializer for <see cref="RulePrefix"/>
        /// </summary>
        static Firewall()
        {
            RulePrefix = Assembly.GetExecutingAssembly().GetName().Name;
        }

        /// <summary>
        /// Gets the firewall policy object
        /// </summary>
        /// <returns>Firewall policy object</returns>
        private static INetFwPolicy2 GetPolicy()
        {
            return (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        }

        /// <summary>
        /// Removes all rules with the current prefix
        /// </summary>
        public static void ClearRules()
        {
            var Policy = GetPolicy();
            var Rules = Policy.Rules
                .Cast<INetFwRule2>()
                .Where(m => m.Name.StartsWith($"{RulePrefix}."))
                .Select(m => m.Name)
                .ToArray();
            foreach (var Rule in Rules)
            {
                Policy.Rules.Remove(Rule);
            }
        }

        /// <summary>
        /// Gets all Addresses for a specific Direction
        /// </summary>
        /// <param name="D">Direction</param>
        /// <returns>Addresses</returns>
        public static CIDR[] GetBlockedAddresses(Direction D)
        {
            if (D == Direction.DISABLED)
            {
                return null;
            }
            if (D == Direction.BOTH)
            {
                return GetBlockedAddresses(Direction.IN).Concat(GetBlockedAddresses(Direction.OUT)).ToArray();
            }
            var Policy = GetPolicy();
            return Policy.Rules
                .Cast<INetFwRule2>()
                .Where(m => m.Name.StartsWith($"{RulePrefix}.{D}.") && m.Direction == (D == Direction.IN ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT))
                .SelectMany(m => m.RemoteAddresses.Split(',').Select(n => new CIDR(n)))
                .ToArray();
        }

        /// <summary>
        /// Gets all addresses in current inbound and outbound rules
        /// </summary>
        /// <returns><see cref="RangeSet"/> array with exactly two entries, <see cref="Direction.IN"/> and <see cref="Direction.OUT"/></returns>
        public static RangeSet[] GetAllRules()
        {
            return new RangeSet[] {
                new RangeSet()
                {
                    Direction = Direction.IN,
                    Ranges = GetBlockedAddresses(Direction.IN)
                },
                new RangeSet()
                {
                    Direction = Direction.OUT,
                    Ranges = GetBlockedAddresses(Direction.OUT)
                }
            };
        }

        /// <summary>
        /// Blocks a range list
        /// </summary>
        /// <param name="Ranges">Range list</param>
        public static void BlockRanges(IEnumerable<RangeSet> Ranges)
        {
            var IN = Ranges.Where(m => m.Direction.HasFlag(Direction.IN)).SelectMany(m => m.Ranges);
            var OUT = Ranges.Where(m => m.Direction.HasFlag(Direction.OUT)).SelectMany(m => m.Ranges);
            IpBlock(IN, Direction.IN);
            IpBlock(OUT, Direction.OUT);
        }

        /// <summary>
        /// Blocks an IP list into the given direction
        /// </summary>
        /// <param name="IPList">IP list</param>
        /// <param name="D">Direction</param>
        private static void IpBlock(IEnumerable<CIDR> IPList, Direction D)
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
                for (var IpCount = 0; IpCount < IPList.Count(); IpCount += BLOCKSIZE)
                {
                    var R = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule"));

                    R.Name = $"{RulePrefix}.{D}.{(IpCount + BLOCKSIZE) / BLOCKSIZE}";
                    R.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                    R.Description = $"{RulePrefix} Rule added on {DateTime.UtcNow}";
                    R.Direction = D == Direction.IN ? NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN : NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                    R.Enabled = true;
                    R.InterfaceTypes = "All";
                    R.LocalAddresses = "*";
                    R.Profiles = int.MaxValue;
                    R.Protocol = 256; //Any
                    R.RemoteAddresses = string.Join(",", IPList.Skip(IpCount).Take(BLOCKSIZE).Select(m => m.ToString()).ToArray());
                    Policy.Rules.Add(R);
                }
            }
        }
    }

    /// <summary>
    /// Firewall Direction of a Rule
    /// </summary>
    [Flags]
    public enum Direction
    {
        /// <summary>
        /// This Range is disabled and will not be applied
        /// </summary>
        DISABLED = 0,
        /// <summary>
        /// Inbound Rule
        /// </summary>
        IN = 1,
        /// <summary>
        /// Outbound Rule
        /// </summary>
        OUT = 2,
        /// <summary>
        /// Rule in both Directions
        /// </summary>
        BOTH = IN | OUT
    }

    /// <summary>
    /// Container for Ranges
    /// </summary>
    public class RangeSet
    {
        /// <summary>
        /// CIDR Range
        /// </summary>
        public CIDR[] Ranges;
        /// <summary>
        /// Rule Direction
        /// </summary>
        public Direction Direction;
    }

}

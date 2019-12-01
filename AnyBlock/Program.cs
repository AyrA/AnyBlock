using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WinAPI.NET;
using static AnyBlock.Logger;

namespace AnyBlock
{
    class Program
    {
        /// <summary>
        /// Exit Codes
        /// </summary>
        private struct ERR
        {
            /// <summary>
            /// Success
            /// </summary>
            public const int SUCCESS = 0;
            /// <summary>
            /// Problem downloading Cache
            /// </summary>
            public const int DOWNLOAD = SUCCESS + 1;
            /// <summary>
            /// Problem applying Rules
            /// </summary>
            public const int RULE_ERROR = DOWNLOAD + 1;
            /// <summary>
            /// Error parsing Arguments
            /// </summary>
            public const int ARGS = RULE_ERROR + 1;
            /// <summary>
            /// Help shown
            /// </summary>
            public const int HELP = 0xFF;
        }

        [STAThread]
        static int Main(string[] args)
        {
#if !DEBUG
            //Handle "/v" Argument. In debug mode, it's always active
            Verbose = args.Any(m => m.ToLower() == "/v");
#endif
            args = args
                .Where(m => m.ToLower() != "/v")
                //.Concat(new string[] { "/add", "IN", "asn", "OVH" })
                .ToArray();


            if (!Cache.HasCache)
            {
                Log("Cache not found. Obtaining now...");
                if (!GetCache())
                {
                    Log("Unable to obtain cache");
                    return ERR.DOWNLOAD;
                }
            }
            else if (!Cache.CacheRecent)
            {
                Log("Cache not recent. Obtaining now...");
                if (!GetCache())
                {
                    Log("Unable to obtain cache");
                    return ERR.DOWNLOAD;
                }
            }
            else
            {
                Debug("Cache found and recent. Using existing");
            }

            if (args.Length == 0)
            {
                Debug("No Command Line Arguments. Starting GUI");
                ShowConfigForm();
                return ERR.SUCCESS;
            }
            if (args.Contains("/?"))
            {
                ShowHelp();
                return ERR.HELP;
            }
            else
            {
                if (args.Length == 1)
                {
                    switch (args[0].ToLower())
                    {
                        case "/apply":
                            return ApplyRules();
                        case "/clear":
                            return ClearRules();
                        case "/config":
                            return PrintConfig();
                        case "/list":
                            return ListAvailableRanges();
                    }
                }
                else
                {
                    switch (args[0].ToLower())
                    {
                        case "/remove":
                            return RemoveCacheItem(args.Skip(1));
                        case "/add":
                            return AddCacheItem(args.Skip(1).FirstOrDefault(), args.Skip(2));
                        case "/export":
                            return ExportConfigRanges(args[1]);
                    }
                }
            }
            Log("Invalid Command Line Arguments. Try /?");
            return ERR.ARGS;
        }

        /// <summary>
        /// Clears firewall rules
        /// </summary>
        /// <returns></returns>
        private static int ClearRules()
        {
            Log("Clearing firewall rules...");
            Firewall.ClearRules();
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Applies firewall rules
        /// </summary>
        private static int ApplyRules()
        {
            Log("Applying firewall Rules...");
            try
            {
                var FWRanges = Cache.SelectedRanges
                    .Select(m => new RangeSet()
                    {
                        Direction = m.Direction,
                        Ranges = Cache.GetAddresses(m).Select(n => new CIDR(n, true)).ToArray()
                    })
                    .ToArray();
                Debug("Clearing existing firewall rules...");
                Firewall.ClearRules();
                Debug("Adding new rules...");
                Firewall.BlockRanges(FWRanges);
                Log("Blocked {0} ranges", FWRanges.SelectMany(m => m.Ranges).Count());
            }
            catch (Exception ex)
            {
                Log("Error: {0}", ex.Message);
                return ERR.RULE_ERROR;
            }
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Prints the current range configuration
        /// </summary>
        private static int PrintConfig()
        {
            foreach (var R in Cache.SelectedRanges)
            {
                Console.WriteLine(R);
            }
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Lists all existing IP ranges
        /// </summary>
        private static int ListAvailableRanges()
        {
            foreach (var E in Cache.ValidEntries)
            {
                Console.WriteLine(E);
            }
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="ItemPath">Item path</param>
        private static int RemoveCacheItem(IEnumerable<string> ItemPath)
        {
            var Cached = Cache.SelectedRanges;
            var FullName = ItemPath.ToArray();
            if (Cached.Any(m => m.Segments.SequenceEqual(FullName)))
            {
                Cached = Cached
                    .Where(m => !m.Segments.SequenceEqual(FullName))
                    .ToArray();
                Log("Range Removed: {0}", string.Join(" --> ", FullName));
            }
            else
            {
                Log("Range '{0}' not in current List", string.Join(" --> ", FullName));
                return ERR.ARGS;
            }
            Cache.SelectedRanges = Cached;
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Adds an item to the cache
        /// </summary>
        /// <param name="Direction">Direction to ignore traffic from/to</param>
        /// <param name="ItemPath">Item path</param>
        private static int AddCacheItem(string Direction, IEnumerable<string> ItemPath)
        {
            var Cached = new List<RangeEntry>(Cache.SelectedRanges);
            if (Direction != null)
            {
                if (ItemPath != null)
                {
                    var FullName = ItemPath.ToArray();
                    if (Cache.ValidEntry(FullName))
                    {
                        var R = new RangeEntry();
                        R.Segments = FullName;
                        if (Enum.TryParse(Direction, true, out R.Direction))
                        {
                            if (Cached.Any(m => m.Segments.SequenceEqual(FullName)))
                            {
                                //Update range
                                Cached = new List<RangeEntry>(Cached
                                    .Where(m => m.Segments.SequenceEqual(FullName))
                                    .Concat(new RangeEntry[] { R }));
                                Log("Updated Range: {0}", string.Join(" --> ", FullName));
                            }
                            else
                            {
                                //Add range
                                Cached.Add(R);
                                Log("Added Range: {0}", string.Join(" --> ", FullName));
                            }
                        }
                        else
                        {
                            Log("Invalid Direction: '{0}'", Direction);
                            return ERR.ARGS;
                        }
                    }
                    else
                    {
                        Log("Name {0} is not a valid Range. Use /list to view all", string.Join(" --> ", FullName));
                        return ERR.ARGS;
                    }
                }
                else
                {
                    Log("No range specified to ignore");
                    return ERR.ARGS;
                }
            }
            else
            {
                Log("No direction specified. Please specify a direction.");
                return ERR.ARGS;
            }
            Cache.SelectedRanges = Cached.ToArray();
            return ERR.SUCCESS;
        }

        /// <summary>
        /// Exports currently configured ranges into a specified format
        /// </summary>
        /// <param name="ExportFormat">Export format</param>
        /// <remarks>Will not read currently used firewall rules, only current configuration</remarks>
        private static int ExportConfigRanges(string ExportFormat)
        {
            if (string.IsNullOrEmpty(ExportFormat))
            {
                Log("No export format specified");
                return ERR.ARGS;
            }
            var Ranges = Cache.SelectedRanges
                .Select(m => new
                {
                    Range = m,
                    Addr = Cache.GetAddresses(m).Select(n => new CIDR(n, true)).ToArray()
                })
                .ToArray();
            switch (ExportFormat.ToLower())
            {
                case "json":
                    var Dict = new Dictionary<string, string[]>();
                    foreach (var R in Ranges)
                    {
                        Dict[string.Join(" ", R.Range.Segments)] = R.Addr.Select(m => m.ToString()).ToArray();
                    }
                    Console.WriteLine(Dict.ToJson(true));
                    break;
                case "csv":
                    //Get delimiter of current locale
                    var Delim = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                    Console.WriteLine("\"name\"{0}\"start\"{0}\"end\"{0}\"cidr\"", Delim);
                    foreach (var R in Ranges)
                    {
                        foreach (var A in R.Addr)
                        {
                            Console.WriteLine("\"{1}\"{0}\"{2}\"{0}\"{3}\"{0}\"{4}\"",
                                Delim,
                                string.Join(" --> ", R.Range.Segments).Replace('"', ' '),
                                A.AddressLow, A.AddressHigh, A);
                        }
                    }
                    break;
                case "tsv":
                    Console.WriteLine("name\tstart\tend\tcidr");
                    foreach (var R in Ranges)
                    {
                        foreach (var A in R.Addr)
                        {
                            Console.WriteLine("{1}\t{2}\t{3}\t{4}",
                                string.Join(" --> ", R.Range.Segments).Replace('\t', ' '),
                                A.AddressLow, A.AddressHigh, A);
                        }
                    }
                    break;
                case "p2p":
                    foreach (var R in Ranges)
                    {
                        foreach (var A in R.Addr)
                        {
                            Console.WriteLine("{0}:{1}-{2}",
                                string.Join(" --> ", R.Range.Segments).Replace(':', ' '),
                                A.AddressLow,
                                A.AddressHigh);
                        }
                    }
                    break;
                default:
                    Log("Unsupported output format: '{0}'", ExportFormat);
                    return ERR.ARGS;
            }
            return ERR.SUCCESS;
        }

        private static bool GetCache()
        {
            var Locker = new object();
            bool Wait = true;
            Exception ex = null;
            Cache.DownloadCacheAsync(delegate (DownloadStatusEventArgs e)
            {
                lock (Locker)
                {
                    Console.CursorLeft = 0;
                    if (e.CanCalculate)
                    {
                        Console.Write("{0:N1}M/{1:N1}M ({2:N2}%)", e.BytesLoaded / 1e6, e.BytesTotal / 1e6, e.Percentage);
                    }
                    else
                    {
                        Console.Write("{0:N1}M", e.BytesLoaded / 1e6);
                    }
                }
                if (e.Complete || e.Error != null)
                {
                    Console.WriteLine();
                    ex = e.Error;
                    Wait = false;
                }
            });
            while (Wait)
            {
                System.Threading.Thread.Sleep(100);
            }
            if (ex != null)
            {
                Console.WriteLine("[{0}]: {1}", ex.GetType().Name, ex.Message);
            }
            return ex == null;
        }

        private static void ShowHelp()
        {
            Console.Error.WriteLine(@"AnyBlock.exe [/v] [/clear | /config | /add dir name | /remove name | /apply | /list | /export <format>]
Blocks IP ranges in the Windows firewall

Shows a graphical configuration window if no arguments are specified.

/v       - Verbose logging to console
/config  - Show currently configured ranges
/add     - Adds the specified range(s) to the list
/remove  - Removes the specified range from the list
/apply   - Applies list to firewall rules
/clear   - Removes all AnyBlock rules
/list    - Lists all available ranges
/export  - Export the currently selected rule ranges

Detailed Help:

/v
Verbose logging.
Shows verbose output in the console.
Can substantially increase runtime.

/config
This simply lists the current configuration.
The format is 'DIR: NAME'
DIR is the direction and is one of IN,OUT,BOTH,DISABLED
NAME is the fully qualified range name. Segments are separated using '-->'

/add dir name
- dir is the direction (same values as in /config)
- name is the fully qualified node name.
Disabled entries are not processed.
It's not necessary to disable an entry to delete it.
To change an existing entry, you can add it again using a different direction.
For the format of the name parameter, see the remove help below.

/remove name
Removing rules from the list is done by name only.
the 'name tree' is supplied as a space delimited list.
To remove TOR exit nodes you would use the arguments /remove tor tor Exit
You can only remove one entry at a time.
To remove all entries, simply delete the 'settings.json' file

/apply
Applying the List will remove all blocked IPs that are no longer in the
current list of addresses.
To get most out of this command, schedule this as a task to be run every
24 hours.

/clear
Removes all rules from the firewall without deleting them from the settings.
To remove them from the settings, use the /remove command.

/list
Lists all available rules.
Be careful, this list has thousands of entries.

/export {csv|tsv|p2p|json}
Exports the currently selected rules with IP ranges in various formats:
csv: Exports name, start-IP, end-IP, cidr in csv format (with headers)
tsv: Same as csv, but uses tab to delimit fields
p2p: Peer to peer blocklist format for common P2P applications.
json: JSON object. Range names are keys and the CIDR list the values
");
        }

        private static void ShowConfigForm()
        {
            if (!Verbose)
            {
                NativeMethods.FreeConsole();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
            Debug("Form Closed");
        }
    }
}

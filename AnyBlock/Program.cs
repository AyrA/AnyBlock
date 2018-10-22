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
            public const int RULE_ERROR = 2;
            /// <summary>
            /// Error parsing Arguments
            /// </summary>
            public const int ARGS = 3;
            /// <summary>
            /// Help shown
            /// </summary>
            public const int HELP = 0xFF;
        }

        [STAThread]
        static int Main(string[] args)
        {
            //Handle "/v" Argument
            Verbose = args.Any(m => m.ToLower() == "/v");
            args = args.Where(m => m.ToLower() != "/v").ToArray();

            if (!Cache.HasCache)
            {
                Debug("Cache not found. Obtaining now");
                if (!Cache.DownloadCache())
                {
                    Log("Unable to obtain cache");
                    return ERR.DOWNLOAD;
                }
            }
            else if (!Cache.CacheRecent)
            {
                Debug("Cache not recent. Obtaining now");
                if (!Cache.DownloadCache())
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
                    if (args[0].ToLower() == "/apply")
                    {
                        Log("Applying Firewall Rules...");
                        try
                        {
                            var FWRanges = Cache.SelectedRanges
                                .Select(m => new RangeSet()
                                {
                                    Direction = m.Direction,
                                    Ranges = Cache.GetAddresses(m.Name).Select(n => new CIDR(n)).ToArray()
                                })
                                .ToArray();
                            Firewall.ClearRules();
                            Firewall.BlockRanges(FWRanges);
                        }
                        catch (Exception ex)
                        {
                            Log("Error: {0}", ex.Message);
                            return ERR.RULE_ERROR;
                        }
                        return ERR.SUCCESS;
                    }
                    if (args[0].ToLower() == "/clear")
                    {
                        Log("Clearing Firewall Rules...");
                        Firewall.ClearRules();
                        return ERR.SUCCESS;
                    }
                    if (args[0].ToLower() == "/config")
                    {
                        foreach (var R in Cache.SelectedRanges)
                        {
                            Console.WriteLine(R.ToString().Replace(" ", ""));
                        }
                        return ERR.SUCCESS;
                    }
                    if (args[0].ToLower() == "/list")
                    {
                        foreach (var E in Cache.ValidEntries)
                        {
                            Console.WriteLine(E);
                        }
                        return ERR.SUCCESS;
                    }
                }
                else
                {
                    if (args[0].ToLower() == "/remove")
                    {
                        var Cached = Cache.SelectedRanges;
                        foreach (var Arg in args.Skip(1))
                        {
                            if (Cached.Any(m => m.Name == Arg))
                            {
                                Cached = Cached.Where(m => m.Name != Arg).ToArray();
                                Log("Range Removed: {0}", Arg);
                            }
                            else
                            {
                                Log("Range {0} not in current List", Arg);
                            }
                        }
                        Cache.SelectedRanges = Cached;
                        return ERR.SUCCESS;
                    }
                    if (args[0].ToLower() == "/add")
                    {
                        var Cached = new List<RangeEntry>(Cache.SelectedRanges);

                        foreach (var Arg in args.Skip(1))
                        {
                            if (Arg.Contains(':'))
                            {
                                var R = new RangeEntry();
                                R.Name = Arg.Substring(Arg.IndexOf(':') + 1);
                                if (Cache.ValidEntry(R.Name))
                                {
                                    if (Enum.TryParse(Arg.Split(':')[0], out R.Direction))
                                    {
                                        if (Cached.Any(m => m.Name == R.Name))
                                        {
                                            Cached = new List<RangeEntry>(Cached.Where(m => m.Name != R.Name).Concat(new RangeEntry[] { R }));
                                            Log("Updated Range: {0}", R.Name);
                                        }
                                        else
                                        {
                                            Cached.Add(R);
                                            Log("Added Range: {0}", R.Name);
                                        }
                                    }
                                    else
                                    {
                                        Log("Invalid Direction in {0}", Arg);
                                    }
                                }
                                else
                                {
                                    Log("Name {0} is not a valid Range. Use /list to view all", R.Name);
                                }
                            }
                        }
                        Cache.SelectedRanges = Cached.ToArray();
                        return ERR.SUCCESS;
                    }
                }
            }
            Log("Invalid Command Line Arguments. Try /?");
            return ERR.ARGS;
        }

        private static void ShowHelp()
        {
            Console.Error.WriteLine(@"AnyBlock.exe [/v] [/clear | /config | /add range [...] | /remove name [...] | /apply | /list]
Blocks IP ranges in the Windows Firewall

Shows a graphical Configuration Window if no Arguments are specified.

/v       - Verbose Logging to console
/config  - Show currently configured Ranges
/add     - Adds the specified Range(s) to the List
/remove  - Removes the specified Range(s) from the List
/apply   - Applies List to Firewall Rules
/clear   - Removes all AnyBlock Rules
/list    - Lists all available Ranges

/add
A range is formatted as dir:name
- dir is the direction and can be IN,OUT,BOTH,DISABLED
- name is the fully qualified node name.
Disabled entries are not processed.
It's not necessary to disable an entry to delete it.
To change an existing entry, you can add it again using a different direction.


/remove
Removing rules from the list is done by name only

/apply
Applying the List will remove all blocked IPs that are no longer in the
current List of Addresses.
To get most out of this command, schedule this as a Task to be run every
24 Hours.");
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

using System;
using System.Linq;
using System.Windows.Forms;
using WinAPI;

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
            /// Help shown
            /// </summary>
            public const int HELP = 0xFF;
        }
        [STAThread]
        static int Main(string[] args)
        {
            if (!Cache.HasCache)
            {
                Console.Error.WriteLine("Cache not found. Obtaining now");
                if (!Cache.DownloadCache())
                {
                    Console.Error.WriteLine("Unable to obtain cache");
                    return ERR.DOWNLOAD;
                }
            }
            else if (!Cache.CacheRecent)
            {
                Console.Error.WriteLine("Cache not recent. Obtaining now");
                if (!Cache.DownloadCache())
                {
                    Console.Error.WriteLine("Unable to obtain cache");
                    return ERR.DOWNLOAD;
                }
            }
            else
            {
                Console.Error.WriteLine("Cache found and recent. Using existing");
            }

            //TODO: Command line argument processing here
            if (args.Length == 0)
            {
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
                if (args.Length == 1 && args[0].ToLower() == "/apply")
                {
                    try
                    {
                        Firewall.ClearRules();
                        Firewall.BlockRanges(Cache.SelectedRanges);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error: {0}", ex.Message);
                        return ERR.RULE_ERROR;
                    }
                }
                if (args.Length == 1 && args[0].ToLower() == "/config")
                {
                    foreach (var R in Cache.SelectedRanges)
                    {
                        Console.Error.WriteLine(R.ToString().Replace(" ", ""));
                    }
                }
                if (args.Length == 1 && args[0].ToLower() == "/list")
                {
                    foreach (var E in Cache.ValidEntries)
                    {
                        Console.Error.WriteLine(E);
                    }
                }
            }
            return ERR.SUCCESS;
        }

        private static void ShowHelp()
        {
            Console.Error.WriteLine(@"AnyBlock.exe [/config | /add entry [...] | /remove entry [...] | /apply | /list]
Blocks IP ranges in the Windows Firewall

Shows a graphical Configuration Window if no Arguments are specified.

/config  - Show currently configured Ranges
/add     - Adds the specified Range(s) to the List
/remove  - Removes the specified Range(s) from the List
/apply   - Applies List to Firewall Rules
/list    - Lists all available Ranges

A range is formatted as dir:name
dir is the direction and can be IN,OUT,BOTH
name is the fully qualified node name.

To change an existing entry, you can add it again using a new direction.");
        }

        private static void ShowConfigForm()
        {
            NativeMethods.FreeConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}

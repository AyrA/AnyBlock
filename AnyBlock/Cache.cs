using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace AnyBlock
{
    public struct RangeEntry
    {
        public string Name;
        public Direction Direction;

        public override string ToString()
        {
            return $"{Direction}: {Name}";
        }
    }

    [Flags]
    public enum Direction
    {
        DISABLED = 0,
        IN = 1,
        OUT = 2,
        BOTH = IN | OUT
    }
    public static class Cache
    {
        public static readonly string CacheFile;

        public static readonly string SettingsFile;

        public static bool HasCache
        {
            get
            {
                return File.Exists(CacheFile);
            }
        }

        public static bool CacheRecent
        {
            get
            {
                return HasCache && File.GetLastWriteTimeUtc(CacheFile) > DateTime.UtcNow.AddDays(-1);
            }
        }

        private static JObject CacheContent;

        public static RangeEntry[] SelectedRanges
        {
            get
            {
                if (File.Exists(SettingsFile))
                {
                    return JsonConvert.DeserializeObject<RangeEntry[]>(File.ReadAllText(SettingsFile));
                }
                return new RangeEntry[0];
            }
            set
            {
                File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(value.Where(m => ValidEntry(m.Name)).ToArray()));
            }
        }

        public static string[] ValidEntries
        {
            get
            {
                var All = new List<string>();
                if (HasCache)
                {
                    if (CacheContent == null)
                    {
                        CacheContent = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(CacheFile));
                    }
                    Stack<JToken> Entries = new Stack<JToken>(CacheContent.Children());
                    while (Entries.Count > 0)
                    {
                        var Current = Entries.Pop();
                        if (Current is JProperty || Current is JObject)
                        {
                            All.Add(Current.Path);
                            foreach (var E in Current.Children<JToken>())
                            {
                                Entries.Push(E);
                            }
                        }
                    }
                }
                return All.Distinct().OrderBy(m => m).ToArray();
            }
        }

        static Cache()
        {
            using (var P = Process.GetCurrentProcess())
            {
                CacheFile = Path.Combine(Path.GetDirectoryName(P.MainModule.FileName), "cache.json");
                SettingsFile = Path.Combine(Path.GetDirectoryName(P.MainModule.FileName), "settings.json");
            }
        }

        public static bool ValidEntry(string Entry)
        {
            if (HasCache)
            {
                if (CacheContent == null)
                {
                    try
                    {
                        CacheContent = (JObject)JsonConvert.DeserializeObject(File.ReadAllText(CacheFile));
                    }
                    catch
                    {
                        return false;
                    }
                }
                var Temp = CacheContent;
                foreach (var key in Entry.Split('.'))
                {
                    if (Temp != null && Temp[key] != null)
                    {
                        //The lowest level doesn't has JObject but JArray
                        if (Temp[key].GetType() == typeof(JObject))
                        {
                            Temp = (JObject)Temp[key];
                        }
                        else
                        {
                            //If we enter the loop again after this, the entry has more than 3 levels
                            Temp = null;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static string[] GetAddresses(params string[] EntryNames)
        {
            return GetAddresses(EntryNames.AsEnumerable());
        }

        public static string[] GetAddresses(IEnumerable<string> EntryNames)
        {
            Console.Error.WriteLine("Validating Names...");
            var Names = EntryNames.Where(m => ValidEntry(m)).ToArray();
            var Addr = new List<string>();
            Console.Error.WriteLine("Getting all matching Nodes...");
            var Nodes = Names
                .SelectMany(m => CacheContent.SelectToken(m, false))
                .Where(m => m != null)
                .ToArray();
            foreach (var Node in Nodes)
            {
                string[] Values = new string[0];
                if (Node is JProperty)
                {
                    Console.Error.WriteLine("Processing {0}...", Node.Path);
                    var Prop = (JProperty)Node;
                    Values = Prop.Descendants()
                        .OfType<JValue>()
                        .Select(m => m.ToString())
                        .ToArray();
                }
                else if(Node is JValue)
                {
                    Values = new string[] { Node.ToString() };
                }
                else if (Node is JArray)
                {
                    var A = (JArray)Node;
                    Values = A.Values().Select(m => m.ToString()).ToArray();
                }
                Addr.AddRange(Values);
            }
            return Addr.Distinct().ToArray();
        }

        public static void Invalidate()
        {
            if (HasCache)
            {
                File.SetLastWriteTimeUtc(CacheFile, DateTime.UtcNow.AddDays(-2));
                CacheContent = null;
            }
        }

        public static async Task DownloadCacheAsync()
        {
            if (!CacheRecent)
            {
                using (var WC = new WebClient())
                {
                    WC.Headers.Add("User-Agent: AnyBlock/1.0 +https://github.com/AyrA/AnyBlock");
                    await WC.DownloadFileTaskAsync("https://cable.ayra.ch/ip/global.json", CacheFile);
                }
            }
        }

        public static bool DownloadCache(int Timeout = System.Threading.Timeout.Infinite)
        {
            var T = DownloadCacheAsync();
            T.Wait(Timeout);
            return !T.IsFaulted && T.IsCompleted && !T.IsCanceled;
        }
    }
}

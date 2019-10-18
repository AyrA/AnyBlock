using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using WinAPI.NET;
using static AnyBlock.Logger;

namespace AnyBlock
{
    public delegate void DownloadStatusHandler(DownloadStatusEventArgs e);

    public class DownloadStatusEventArgs
    {
        public long BytesLoaded { get; private set; }
        public long BytesTotal { get; private set; }
        public bool CanCalculate { get; private set; }
        public double Percentage
        {
            get
            {
                return CanCalculate ? BytesLoaded * 100.0 / BytesTotal : -1.0;
            }
        }
        public bool Cancel { get; set; }
        public bool Complete { get; private set; }
        public Exception Error { get; private set; }

        public DownloadStatusEventArgs(long BytesTotal, Exception Error)
        {
            CanCalculate = Complete = true;
            this.Error = Error;
            BytesLoaded = this.BytesTotal = BytesTotal;
        }

        public DownloadStatusEventArgs(long BytesLoaded, long BytesTotal)
        {
            this.BytesLoaded = BytesLoaded;
            this.BytesTotal = BytesTotal;
            CanCalculate = BytesTotal >= 0;
        }
    }

    public struct RangeEntry
    {
        public string[] Segments;
        public Direction Direction;

        public override string ToString()
        {
            var N = string.Join(" --> ", Segments);
            return $"{Direction}: {N}";
        }
    }

    public static class Cache
    {
        public static readonly string CacheFile;

        public static readonly string SettingsFile;

        public static bool HasCache
        {
            get
            {
                return File.Exists(CacheFile) && CacheSize > 0;
            }
        }

        public static bool CacheRecent
        {
            get
            {
                return HasCache && File.GetLastWriteTimeUtc(CacheFile) > DateTime.UtcNow.AddDays(-1);
            }
        }

        private static long CacheSize
        {
            get
            {
                var FI = new FileInfo(CacheFile);
                if (FI.Exists)
                {
                    return FI.Length;
                }
                throw new FileNotFoundException("Cache does not exist yet", CacheFile);
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
                File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(value.Where(m => ValidEntry(m.Segments)).ToArray()));
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

        public static bool ValidEntry(string[] Segments)
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
                foreach (var key in Segments)
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

        public static string[] GetAddresses(params RangeEntry[] EntryNames)
        {
            return GetAddresses(EntryNames.AsEnumerable());
        }

        public static string[] GetAddresses(IEnumerable<RangeEntry> EntryNames)
        {
            Debug("Validating Names...");
            var Names = EntryNames.Where(m => ValidEntry(m.Segments)).ToArray();
            var Addr = new List<string>();
            Debug("Getting all matching Nodes...");
            var Nodes = Names
                .SelectMany(m => SelectToken(m.Segments))
                .Where(m => m != null)
                .ToArray();
            foreach (var Node in Nodes)
            {
                string[] Values = new string[0];
                if (Node is JProperty)
                {
                    Debug("Processing {0}...", Node.Path);
                    var Prop = (JProperty)Node;
                    Values = Prop.Descendants()
                        .OfType<JValue>()
                        .Select(m => m.ToString())
                        .ToArray();
                }
                else if (Node is JValue)
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
                try
                {
                    File.SetLastWriteTimeUtc(CacheFile, DateTime.UtcNow.AddDays(-2));
                }
                catch (Exception exTime)
                {
                    try
                    {
                        File.Delete(CacheFile);
                    }
                    catch (Exception exDel)
                    {
                        throw new AggregateException("Unable to invalidate the cache. See inner exceptions for details", exTime, exDel);
                    }
                }
                CacheContent = null;
            }
        }

        public static void DownloadCacheAsync(DownloadStatusHandler Handler = null)
        {
            if (!CacheRecent)
            {
                var WC = new DecompressClient();
                if (Handler != null)
                {
                    WC.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e)
                    {
                        var Evt = new DownloadStatusEventArgs(e.BytesReceived, e.TotalBytesToReceive);
                        Handler(Evt);
                        if (Evt.Cancel)
                        {
                            WC.CancelAsync();
                            WC.Dispose();
                        }
                    };

                    WC.DownloadFileCompleted += delegate (object sender, AsyncCompletedEventArgs e)
                    {
                        long Size = -1;
                        try
                        {
                            Size = CacheSize;
                        }
                        catch
                        {

                        }
                        var Evt = new DownloadStatusEventArgs(Size, Size == 0 ? new WebException("Unable to download cache") : e.Error);
                        Handler(Evt);
                        WC.Dispose();
                    };
                }
                WC.Headers.Add("User-Agent: AnyBlock/1.0 +https://github.com/AyrA/AnyBlock");
                WC.DownloadFileAsync(new Uri("https://cable.ayra.ch/ip/global.json"), CacheFile);
            }
        }

        private static JToken[] SelectToken(IEnumerable<string> PathSegments)
        {
            var X = (JToken)CacheContent;
            foreach (var P in PathSegments)
            {
                X = X[P];
                if (X == null)
                {
                    return null;
                }
            }
            return X.Children().ToArray();
        }
    }
}

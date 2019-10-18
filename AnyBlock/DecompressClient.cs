using System;
using System.Linq;
using System.Net;

namespace AnyBlock
{
    /// <summary>
    /// WebClient with GZip and Deflate support
    /// </summary>
    public class DecompressClient : WebClient
    {
        private static DecompressionMethods _all = DecompressionMethods.None;

        private static DecompressionMethods AllMethods
        {
            get
            {
                if (_all == DecompressionMethods.None)
                {
                    _all = (DecompressionMethods)Enum.GetValues(typeof(DecompressionMethods))
                        .OfType<DecompressionMethods>()
                        .Cast<int>()
                        .Sum();
                }
                return _all;
            }
        }

        protected override WebRequest GetWebRequest(Uri address)
        {

            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = AllMethods;
            return request;
        }
    }
}

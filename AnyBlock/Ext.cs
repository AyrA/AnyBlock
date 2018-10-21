using Newtonsoft.Json;

namespace AnyBlock
{
    public static class Ext
    {
        public static string ToJson(this object o, bool Pretty = false)
        {
            return JsonConvert.SerializeObject(o, Pretty ? Formatting.Indented : Formatting.None);
        }

        public static T FromJson<T>(this string s, T Default = default(T))
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(s);
            }
            catch
            {

            }
            return Default;
        }
    }
}

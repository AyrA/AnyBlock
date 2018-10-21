using System.Runtime.InteropServices;

namespace AnyBlock
{
    public static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

    }
}

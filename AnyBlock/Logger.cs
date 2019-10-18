using System;

namespace AnyBlock
{
    public static class Logger
    {
#if DEBUG
        public static bool Verbose = true;
#else
        public static bool Verbose = false;
#endif

        public static void Log(string Message)
        {
            System.Diagnostics.Debug.WriteLine(Message, nameof(Log));
            Console.Error.WriteLine(Message);
        }

        public static void Log(string Message, params object[] Args)
        {
            Log(string.Format(Message, Args));
        }

        public static void Debug(string Message)
        {
            System.Diagnostics.Debug.WriteLine(Message, nameof(Debug));
            if (Verbose)
            {
                Console.Error.WriteLine(Message);
            }
        }

        public static void Debug(string Message, params object[] Args)
        {
            Debug(string.Format(Message, Args));
        }
    }
}

using System;

namespace AnyBlock
{
    public static class Logger
    {
        public static bool Verbose = false;

        public static void Log(string Message)
        {
            Console.Error.WriteLine(Message);
        }

        public static void Log(string Message, params object[] Args)
        {
            Log(string.Format(Message, Args));
        }

        public static void Debug(string Message)
        {
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

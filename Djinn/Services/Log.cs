using System;
using System.Threading;

namespace Djinn.Services
{
    internal enum LogLevel
    {
        Debug,
        Verbose,
        Information,
        Warning,
        Error,
        Silent
    }

    internal static class Log
    {
        private static readonly object lockObj = new object();

        public static LogLevel Level { get; set; } = LogLevel.Information;

        public static void Debug(string message)
        {
            if (Level > LogLevel.Debug)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }
        
        public static void Verbose(string message)
        {
            if (Level > LogLevel.Verbose)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Information(string message)
        {
            if (Level > LogLevel.Information)
                return;

            lock (lockObj)
            {
                Console.WriteLine(message);
            }
        }

        public static void Success(string message)
        {
            if (Level > LogLevel.Information)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Warning(string message)
        {
            if (Level > LogLevel.Warning)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Error(string message)
        {
            if (Level > LogLevel.Error)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void Error(Exception exception, string message)
        {
            if (Level > LogLevel.Error)
                return;

            lock (lockObj)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(exception);
                Console.ResetColor();
            }
        }
    }
}

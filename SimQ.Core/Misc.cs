using System;

namespace SimQCore {

    public enum LogStatus {
        ERROR,
        INFO,
        WARNING,
        SUCCESS
    }

    public static class Misc {
        public static bool showLogs { get; set; } = true;
        public static void Log(string message, LogStatus status = LogStatus.INFO) {
            if (showLogs)
            {
                switch (status)
                {
                    case LogStatus.ERROR:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogStatus.INFO:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                    case LogStatus.WARNING:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    case LogStatus.SUCCESS:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine(message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}

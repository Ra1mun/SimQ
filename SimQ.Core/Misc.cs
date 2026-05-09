using System;
using System.Collections.Generic;

namespace SimQCore {

    public enum LogStatus {
        ERROR,
        INFO,
        WARNING,
        SUCCESS
    }

    public sealed class LogEntry {
        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
        public string Message { get; set; } = "";
    }

    public static class Misc {
        public static bool showLogs { get; set; } = true;

        /// <summary>
        /// In-memory log buffer filled during a simulation run.
        /// Call <see cref="ClearLogBuffer"/> before each run and
        /// <see cref="GetLogBuffer"/> after it finishes.
        /// </summary>
        private static readonly List<LogEntry> _logBuffer = new();
        private static readonly object _logLock = new();

        public static void ClearLogBuffer() { lock (_logLock) _logBuffer.Clear(); }
        public static List<LogEntry> GetLogBuffer() { lock (_logLock) return new List<LogEntry>(_logBuffer); }

        public static void Log(string message, LogStatus status = LogStatus.INFO) {
            // Always buffer so the client can retrieve logs via the API.
            lock (_logLock) {
                _logBuffer.Add(new LogEntry {
                    Timestamp = DateTime.UtcNow,
                    Status = status,
                    Message = message.TrimStart('\n', '\r'),
                });
            }

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

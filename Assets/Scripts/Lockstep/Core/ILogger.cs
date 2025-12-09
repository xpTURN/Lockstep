using System;

namespace xpTURN.Lockstep.Core
{
    /// <summary>
    /// Log level enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 99
    }

    /// <summary>
    /// Logger interface for platform-independent logging
    /// </summary>
    public interface ILockstepLogger
    {
        /// <summary>
        /// Minimum log level to output
        /// </summary>
        LogLevel MinLevel { get; set; }
        
        /// <summary>
        /// Log debug message
        /// </summary>
        void Debug(string message);
        
        /// <summary>
        /// Log info message
        /// </summary>
        void Info(string message);
        
        /// <summary>
        /// Log warning message
        /// </summary>
        void Warning(string message);
        
        /// <summary>
        /// Log error message
        /// </summary>
        void Error(string message);
        
        /// <summary>
        /// Log with format string
        /// </summary>
        void Log(LogLevel level, string format, params object[] args);
    }

    /// <summary>
    /// Default console logger (works without Unity)
    /// </summary>
    public class ConsoleLogger : ILockstepLogger
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public void Debug(string message)
        {
            if (MinLevel <= LogLevel.Debug)
                Console.WriteLine($"[DEBUG] {message}");
        }

        public void Info(string message)
        {
            if (MinLevel <= LogLevel.Info)
                Console.WriteLine($"[INFO] {message}");
        }

        public void Warning(string message)
        {
            if (MinLevel <= LogLevel.Warning)
                Console.WriteLine($"[WARN] {message}");
        }

        public void Error(string message)
        {
            if (MinLevel <= LogLevel.Error)
                Console.WriteLine($"[ERROR] {message}");
        }

        public void Log(LogLevel level, string format, params object[] args)
        {
            if (MinLevel <= level)
            {
                string message = args.Length > 0 ? string.Format(format, args) : format;
                switch (level)
                {
                    case LogLevel.Debug: Debug(message); break;
                    case LogLevel.Info: Info(message); break;
                    case LogLevel.Warning: Warning(message); break;
                    case LogLevel.Error: Error(message); break;
                }
            }
        }
    }

    /// <summary>
    /// Silent logger (no output)
    /// </summary>
    public class NullLogger : ILockstepLogger
    {
        public static readonly NullLogger Instance = new NullLogger();
        
        public LogLevel MinLevel { get; set; } = LogLevel.None;
        
        public void Debug(string message) { }
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
        public void Log(LogLevel level, string format, params object[] args) { }
    }

    /// <summary>
    /// Static logger accessor
    /// </summary>
    public static class LockstepLogger
    {
        private static ILockstepLogger _logger;

        /// <summary>
        /// Current logger instance
        /// </summary>
        public static ILockstepLogger Instance
        {
            get => _logger ?? (_logger = CreateDefaultLogger());
            set => _logger = value ?? NullLogger.Instance;
        }

        /// <summary>
        /// Create default logger based on platform
        /// </summary>
        private static ILockstepLogger CreateDefaultLogger()
        {
#if UNITY_2021_1_OR_NEWER
            return new UnityLogger();
#else
            return new ConsoleLogger();
#endif
        }

        public static void Debug(string message) => Instance.Debug(message);
        public static void Info(string message) => Instance.Info(message);
        public static void Warning(string message) => Instance.Warning(message);
        public static void Error(string message) => Instance.Error(message);
        public static void Log(LogLevel level, string format, params object[] args) 
            => Instance.Log(level, format, args);
    }

#if UNITY_2021_1_OR_NEWER
    /// <summary>
    /// Unity-specific logger implementation
    /// </summary>
    public class UnityLogger : ILockstepLogger
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public void Debug(string message)
        {
            if (MinLevel <= LogLevel.Debug)
                UnityEngine.Debug.Log(message);
        }

        public void Info(string message)
        {
            if (MinLevel <= LogLevel.Info)
                UnityEngine.Debug.Log(message);
        }

        public void Warning(string message)
        {
            if (MinLevel <= LogLevel.Warning)
                UnityEngine.Debug.LogWarning(message);
        }

        public void Error(string message)
        {
            if (MinLevel <= LogLevel.Error)
                UnityEngine.Debug.LogError(message);
        }

        public void Log(LogLevel level, string format, params object[] args)
        {
            if (MinLevel <= level)
            {
                string message = args.Length > 0 ? string.Format(format, args) : format;
                switch (level)
                {
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        UnityEngine.Debug.Log(message);
                        break;
                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(message);
                        break;
                }
            }
        }
    }
#endif
}


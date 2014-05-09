using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FastLogAsync
{
    /// <summary>
    ///     Logging system initializes itself with static constructor.
    ///     Define FAST_LOG compilation symbol to allow using logging in assembly/file
    ///     Define FAST_TRACE to also allow tracing/verbose log
    /// </summary>
    public static class Log
    {
        public static string TimeStampFormat = "HH:mm:ss.fff";
        private const string DateTimeStampFormat = "yyyy-MM-dd HH:mm:ss.fff";
       
        // you can configure this properties at runtime 
        // and/or set AppSettings with the same names in configuration file
        
        // echo everything to console
        public static bool ConsoleOutputEnabled;
        // save log lines to file
        public static bool FileOutputEnabled = true;
        
        public static bool InfoLogEnabled = true;
        public static bool ErrorLogEnabled = true;
        public static bool TraceLogEnabled = true;

        // public static string LogFilePath;

        /// <summary>
        ///     It's best to subscribe this method to 'exiting' events of your application.
        ///     It still tries to exit gracefully, but not all the cases is possible to cover
        ///     without application-specific exit events.
        /// </summary>
        public static void DoGracefullExit()
        {
            try
            {
                LogQueue.CompleteAdding();
                _writingThread.Join();
            }
            catch
            {
                // we rally don't care about exceptions here
            }  
        }

        [Conditional("FAST_LOG")]
        public static void Info(string msg, params object[] par)
        {
            if (!InfoLogEnabled) return;
            LogQueue.Add(string.Format("{0} INF {1}\r\n", DateTime.UtcNow.ToString(TimeStampFormat),
                // need to check because {} symbols might break Format call
                (par == null || par.Length == 0) ? msg : string.Format(msg, par)));
        }

        [Conditional("FAST_LOG")]
        public static void Error(string msg, params object[] par)
        {
            if (!ErrorLogEnabled) return;
            LogQueue.Add(string.Format("{0} ERR {1}\r\n", DateTime.UtcNow.ToString(TimeStampFormat),
                // need to check because {} symbols might break Format call
                (par == null || par.Length == 0) ? msg : string.Format(msg, par)));
        }

        [Conditional("FAST_LOG")]
        public static void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        [Conditional("FAST_TRACE")]
        public static void Trace(string msg, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!TraceLogEnabled) return;

            LogQueue.Add(string.Format("{0} TRC {1}:{2}[{3}] {4}\r\n", DateTime.UtcNow.ToString(TimeStampFormat),
                Path.GetFileName(sourceFilePath), memberName,
                sourceLineNumber, msg));
        }

        // INTERNALS ------------------------------------------------------------
        private static readonly Thread _writingThread;
        private static readonly BlockingCollection<string> LogQueue;
        private static int _currentDay = -1;
        private static StreamWriter _currentFileStream;
        //static constructor
        static Log()
        {
            TryLoadSetting("TimeStampFormat", ref TimeStampFormat);
            TryLoadSetting("ConsoleOutputEnabled", ref ConsoleOutputEnabled);
            TryLoadSetting("FileOutputEnabled", ref FileOutputEnabled);
            TryLoadSetting("InfoLogEnabled", ref InfoLogEnabled);
            TryLoadSetting("ErrorLogEnabled", ref ErrorLogEnabled);
            TryLoadSetting("TraceLogEnabled", ref TraceLogEnabled);

            LogQueue = new BlockingCollection<string>();
            AppDomain.CurrentDomain.ProcessExit += (s, a) => DoGracefullExit();

            _writingThread = new Thread(Write);
            _writingThread.Name = "Log writer thread";
            _writingThread.IsBackground = true;
            _writingThread.Priority = ThreadPriority.BelowNormal;
            _writingThread.Start();

            LogQueue.Add(string.Format("-------------------------------------------------\r\n" +
                                       "{0} Logging system is initialized\r\n" +
                                       "-------------------------------------------------\r\n",
                DateTime.UtcNow.ToString(DateTimeStampFormat)));
        }

        // returns appSetting value if available
        private static void TryLoadSetting(string name, ref bool val)
        {
            bool test;
            if (bool.TryParse(ConfigurationManager.AppSettings[name], out test))
                val = test;
        }
        private static void TryLoadSetting(string name, ref string val)
        {
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[name]))
                val = ConfigurationManager.AppSettings[name];
        }
        // Property handles daily roll-over files
        private static StreamWriter LogFile
        {
            get
            {
                if (DateTime.UtcNow.Day == _currentDay)
                    return _currentFileStream;

                // changing file
                _currentDay = DateTime.UtcNow.Day;
                string dirName = Path.Combine(
                    Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
                    "logs");

                if (!Directory.Exists(dirName))
                    Directory.CreateDirectory(dirName);

                string logFile = Path.Combine(dirName, DateTime.UtcNow.ToString(@"yyMMdd.lo\g"));

                if (_currentFileStream != null)
                    _currentFileStream.Dispose();

                _currentFileStream = new StreamWriter(File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                _currentFileStream.AutoFlush = true;

                return _currentFileStream;
            }
        }

        // Infinite logging thread
        private static void Write()
        {
            string msg;
            try
            {
                while (true)
                {
                    msg = LogQueue.Take();

                    if (FileOutputEnabled)
                        LogFile.Write(msg);

                    if (ConsoleOutputEnabled)
                    {
                        var oldColor = Console.ForegroundColor;
                        var type = msg.Substring(9, 3);
                        switch (type)
                        {
                            case "ERR":
                                Console.ForegroundColor = ConsoleColor.Red;
                                break;
                            case "INF":
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                break;
                        }

                        Console.Write(msg);
                        Console.ForegroundColor = oldColor;
                    }
                }
            }
            catch
            {
                msg = string.Format("---------------------------------------------------\r\n" +
                                    "{0} Logging system is shutting down\r\n" +
                                    "---------------------------------------------------\r\n",
                    DateTime.UtcNow.ToString(DateTimeStampFormat));

                if (FileOutputEnabled)
                    LogFile.Write(msg);

                if (ConsoleOutputEnabled)
                    Console.Write(msg);
            }
        }

        //------------------------------------------------------------
    }
}
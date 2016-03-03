using Crypton.Carbonator.Config;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Provides logging methods for Carbonator
    /// </summary>
    internal static class Log
    {

        enum Types
        {
            None,
            EventLog,
            Log4Net
        }

        private static Types type = Types.None;
        private static ILog log4net = null;

        static Log()
        {
            string logTypeConfig = CarbonatorSection.Current.LogType.ToLowerInvariant();
            switch (logTypeConfig)
            {
                case "log4net":
                    type = Types.Log4Net;
                    break;
                case "eventlog":
                    type = Types.EventLog;
                    break;
                default:
                    type = Types.None;
                    break;
            }

            if (type == Types.Log4Net)
            {
                XmlConfigurator.Configure();
                log4net = LogManager.GetLogger(typeof(Log));
            }
        }

        /// <summary>
        /// Writes a debugging entry to the log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Debug(string message, params object[] args)
        {
            string formatted = string.Format(message, args);
            switch (type)
            {
                case Types.EventLog:
                    // there's no debug in event log
                    break;
                case Types.Log4Net:
                    log4net.Debug(formatted);
                    break;                
            }

            if (Program.ConsoleMode && Program.Verbose)
            {
                Console.WriteLine(formatted);
                System.Diagnostics.Debug.WriteLine(formatted);
            }
        }

        /// <summary>
        /// Writes an informational entry to the log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Info(string message, params object[] args)
        {
            string formatted = string.Format(message, args);
            switch (type)
            {
                case Types.EventLog:
                    EventLog.WriteEntry(Program.EVENT_SOURCE, formatted, EventLogEntryType.Information);
                    break;
                case Types.Log4Net:
                    log4net.Info(formatted);
                    break;
            }

            if (Program.ConsoleMode && Program.Verbose)
            {
                Console.WriteLine("[info] " + formatted);
                System.Diagnostics.Debug.WriteLine(formatted);
            }
        }

        /// <summary>
        /// Writes a warning entry to the log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Warning(string message, params object[] args)
        {
            string formatted = string.Format(message, args);
            switch (type)
            {
                case Types.EventLog:
                    EventLog.WriteEntry(Program.EVENT_SOURCE, formatted, EventLogEntryType.Warning);
                    break;
                case Types.Log4Net:
                    log4net.Warn(formatted);
                    break;
            }

            if (Program.ConsoleMode && Program.Verbose)
            {
                Console.WriteLine("[warn] " + formatted);
                System.Diagnostics.Debug.WriteLine(formatted);
            }
        }

        /// <summary>
        /// Writes an error entry to the log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Error(string message, params object[] args)
        {
            string formatted = string.Format(message, args);
            switch (type)
            {
                case Types.EventLog:
                    EventLog.WriteEntry(Program.EVENT_SOURCE, formatted, EventLogEntryType.Error);
                    break;
                case Types.Log4Net:
                    log4net.Error(formatted);
                    break;
            }

            if (Program.ConsoleMode && Program.Verbose)
            {
                Console.WriteLine("[error] " + formatted);
                System.Diagnostics.Debug.WriteLine(formatted);
            }
        }

        /// <summary>
        /// Writes a fatal entry to the log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Fatal(string message, params object[] args)
        {
            string formatted = string.Format(message, args);
            switch (type)
            {
                case Types.EventLog:
                    EventLog.WriteEntry(Program.EVENT_SOURCE, formatted, EventLogEntryType.Error);
                    break;
                case Types.Log4Net:
                    log4net.Fatal(formatted);
                    break;
            }

            if (Program.ConsoleMode && Program.Verbose)
            {
                Console.WriteLine("[fatal] " + formatted);
                System.Diagnostics.Debug.WriteLine(formatted);
            }
        }

    }
}

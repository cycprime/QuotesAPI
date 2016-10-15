using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using NLog.Common;
using NLog.Targets;
using NLog.Config;
using NLog.Conditions;

namespace QuotesAPI
{

    public class LogConfig
    {

        private readonly Logger _logger;

        private readonly string _defaultFilenameRoot;



        // 
        // Constructor.
        // 
        public LogConfig(Logger logger)
        {

            _logger = logger;

            Type typeLogConfig = typeof(LogConfig);

            _defaultFilenameRoot = typeLogConfig.Namespace;

        }

        // 
        // Set up default configuration for NLog.
        // 
        public LoggingConfiguration SetUp()
        {

            LoggingConfiguration config = new LoggingConfiguration();

            SetUpConsole(config);

            SetupErrorLog(config);

            return config;

        }

        // 
        // Add to the default logging configuration for rules displaying 
        // logs on the console.
        // 
        protected void SetUpConsole(LoggingConfiguration config)
        {

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();

            string targetName = "console";

            config.AddTarget(targetName, consoleTarget);

            consoleTarget.Layout = 
                @"${longdate} ${pad:padding=5:inner=-${uppercase:${level}}-} ${message}";

            LoggingRule consoleRule = new LoggingRule(
                "*", 
//                 LogLevel.Trace,
//                 LogLevel.Debug,
                LogLevel.Info,
                consoleTarget);

            config.LoggingRules.Add(consoleRule);

            consoleTarget.UseDefaultRowHighlightingRules = false;

            ConsoleWordHighlightingRule debugHighlightRule = 
                new ConsoleWordHighlightingRule(
                    "-DEBUG-", 
                    ConsoleOutputColor.DarkBlue,
                    ConsoleOutputColor.Yellow);

            ConsoleWordHighlightingRule infoHighlightRule = 
                new ConsoleWordHighlightingRule(
                    "-INFO-", 
                    ConsoleOutputColor.DarkGreen,
                    ConsoleOutputColor.Black);

            ConsoleWordHighlightingRule warnHighlightRule = 
                new ConsoleWordHighlightingRule(
                    "-WARN-", 
                    ConsoleOutputColor.DarkYellow,
                    ConsoleOutputColor.Black);

            ConsoleWordHighlightingRule errorHighlightRule = 
                new ConsoleWordHighlightingRule(
                    "-ERROR-", 
                    ConsoleOutputColor.Yellow,
                    ConsoleOutputColor.DarkMagenta);

            ConsoleWordHighlightingRule fatalHighlightRule = 
                new ConsoleWordHighlightingRule(
                    "-FATAL-", 
                    ConsoleOutputColor.White,
                    ConsoleOutputColor.DarkRed);

            consoleTarget.WordHighlightingRules.Add(debugHighlightRule);

            consoleTarget.WordHighlightingRules.Add(infoHighlightRule);

            consoleTarget.WordHighlightingRules.Add(warnHighlightRule);

            consoleTarget.WordHighlightingRules.Add(errorHighlightRule);

            consoleTarget.WordHighlightingRules.Add(fatalHighlightRule);

        }

        // 
        // Set up default error logging to a flat file.
        // 
        protected void SetupErrorLog(LoggingConfiguration config)
        {

            FileTarget fileTarget = new FileTarget();

            string targetName = "defaultLogFile";

            config.AddTarget(targetName, fileTarget);

            string contentRootPath = "";

            try 
            {

                contentRootPath = Directory.GetCurrentDirectory();

            }
            catch (UnauthorizedAccessException ex)
            {

                string message = "Application does not have permission to " +
                    "access the current directory: " + ex.Message;

                _logger.Error(message);

                _logger.Error("The application will not write to any logfile.");

                return;

            }
            catch (Exception ex)
            {

                string message = "Application encountered error attempting " +
                    "to write to an error log: " + ex.Message;

                _logger.Error(message);

                _logger.Error("The application will not write to any logfile.");

                return;

            }
            finally
            {

                config.RemoveTarget(targetName);

            }

            contentRootPath = contentRootPath + "/";

            string filename = contentRootPath + 
                "Logging/" + 
                _defaultFilenameRoot + ".log";

            fileTarget.FileName = filename;

            fileTarget.Layout = 
                @"${longdate}|${pad:padding=5:inner=-${uppercase:${level}}-}|${logger}|${message}";

            LoggingRule fileRule = new LoggingRule(
                "*", 
//                 LogLevel.Trace,
                LogLevel.Debug,
                fileTarget);

            config.LoggingRules.Add(fileRule);

        }

    }

}

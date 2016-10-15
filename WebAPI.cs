using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using QuotesAPI.Database.Models;
using QuotesAPI.Models;
using NLog;
using NLog.Config;

namespace QuotesAPI
{

    public class WebAPI
    {

        public const string DefaultConfigFile = "appsettings.json";

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private static IConfigurationRoot _configuration;

        public static IConfigurationRoot Configuration 
        {

            get
            {

                return _configuration;

            }

            protected set
            {

                _configuration = value;

            }

        }

        static Dictionary<string, string> switchMappings { get; } =
            new Dictionary<string, string>()
            {

                {"--mode", "mode"},
                {"-m", "mode"},
                {"--config", "config"},
                {"-c", "config"},
                {"--qid", "qid"},
                {"-q", "qid"},
                {"--seed", "seed"},
                {"-s", "seed"},
                {"--add", "add"},
                {"-a", "add"},

            };

        // 
        // Provides a help manual on the command line argument usage.
        // 
        public static void CmdLineHelp() 
        {

            string helpText = @"
Usage: dotnet run [--qid <quote ID> | --mode <setup|cleanup|api> [--config <dbsetting file> | --seed <seed file>] ]

DESCRIPTION
    Runs a server that service the web api for accessing random quotes, or
    in console mode, set up or clean up the database for the quotes.

    Options are:

    no option   If no options is supplied, the program runs as a daemon
                and services the web api for random quotes.

    --qid <quote ID>
                This options retrieve the quote corresponding to the 
                quote ID specified. 

                To retrieve a quote based on a quote ID, a database
                connection configuration json file needs to be specified
                as well:

                 > dotnet run --config <dbsetting config> --qid <quote id>

    --add <quotes file>
                This option adds quotes that are listed in the <quote file>.
                The quotes should be listed in json format that is similar
                to the seed file:
                [
                  {
                    ""RefId"": ""<External Ref ID>"",
                    ""Content"": ""<Quote Content>"",
                    ""Source"": ""<Source of the Quote>"",
                    ""SourceUrl"": ""<URL for the Source>""
                  },
                ]
                
                To add new quotes to the database via the command line, 
                specify the command as follows:

                 > dotnet run --config <dbsetting config> --add <quote file>



    --mode      Specifies if the program should run as console and set up the 
                database, clean up the database, or run as a web server to 
                service the random quote api.

                To set up the database tables for quotes and seed it with
                same data, use --mode setup:
            
                    >  dotnet run --mode setup --config <filename> [--seed <seed filename>]

                To dispose of the data, and drop all the tables realted to 
                quotes, use --mode cleanup:

                    >  dotnet run --mode cleanup --config <filename>

                To run the web api service, use --mode api or supply no 
                argument:

                    >  dotnet run --mode api

                    >  dotnet run

                Running in ""--mode api"" ignores all subsequent 
                non-""--help"" arguments.

    --config <dbsetting file>
                The config option specifies the location of the configuration
                needed to for database connection to set up or clean up
                the database tables.

    --seed <seed file>
                The seed file provides a list of quotes in a json format.
                The format should be:
                [
                  {
                    ""RefId"": ""<External Ref ID>"",
                    ""Content"": ""<Quote Content>"",
                    ""Source"": ""<Source of the Quote>"",
                    ""SourceUrl"": ""<URL for the Source>""
                  },
                ]

                If the seed file is not specified, the tables will
                not be seeded with any quotes upon initial set up.

                ";

            Console.WriteLine("{0}", helpText);

        }

        // 
        // Starts the web api service.
        // 
        public static void StartService() 
        {

            _logger.Trace("Starting web api service...");

            _logger.Info("Sever URL(s) = {0}.", Configuration["server.urls"]);

            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(Configuration)
                .UseKestrel(options =>
                    {
                        options.NoDelay = true;
                        options.UseHttps("testCert.pfx", "testPassword");
                    }
                )
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();

        }

        // 
        // Set up the database table(s) and seed the table where
        // appropriate.
        // 
        public static void SetupAndSeed()
        {

            _logger.Trace("Set up and seed tables...");

            string cs = null;

            try 
            {

                cs = GetConnectionString();

            }
            catch (System.ArgumentException ex)
            {

                _logger.Error("Error: {0}.", ex.Message);

                string errorMessage = "Cannot get connection string for " +
                    "database, unable to proceed";

                _logger.Fatal(errorMessage);

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }
            catch (Exception ex)
            {

                _logger.Error("Error: {0}.", ex.Message);

                string errorMessage = "Failed to set up database table.";

                _logger.Fatal(errorMessage);

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }

            MySqlConnection conn = new MySqlConnection(cs);

            PrincipalTable.DatabaseConnection = conn;

            bool tableExists = false;

            try 
            {

                tableExists = PrincipalTable.TableExists();

            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {

                _logger.Fatal("Abort operation due to database error: " + 
                    ex.Message);

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }
            catch (Exception ex)
            {

                _logger.Fatal("Abort operation to set up and seed " + 
                    "database due to error: " + ex.Message);

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;
            }

            if (tableExists)
            {

                string errorMessage = "Database table " + 
                    PrincipalTable.Name  + " " +
                    "already exists - " +
                    "table not created.";

                _logger.Warn(errorMessage);

                Console.WriteLine($"No action required - {errorMessage}.");

                return;

            }

            try 
            {

                PrincipalTable.CreateTable();

                PrincipalTable.AddTableTriggers();

            }
            catch (Exception ex)
            {

                string message = "Failed table/trigger creation: " 
                    +  ex.Message;

                _logger.Error(message);

                _logger.Fatal("Abort operation to set up database!");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }

            QuoteInput seed = new QuoteInput();

            string filename = null;

            filename = Configuration["seed"];

            if (null == filename)
            {

                string errorMessage = "No seed file specified - " + 
                    "table not seeded.";

                _logger.Warn(errorMessage);

                Console.WriteLine(
                    "Warn: Database not seeded, missing seed file.");

                return;

            }

            _logger.Info("Seeding newly created database with " + 
                $"file {filename}.");

            int count = PrincipalTable.AddQuotesFromFile(filename);

            if (0 >= count)
            {

                Console.WriteLine("No quotes seeded into database.");

            }
            else 
            {

                Console.WriteLine($"Number of quotes seeded = {count}.");

            }

            return;

        }

        // 
        // Add quotes from a json file that is in the same format as the 
        // seeding file.
        // 
        public static void AddQuotesByFile(IConfiguration config)
        {

            _logger.Trace("Adding new quotes to table...");

            string cs = null;

            try 
            {

                cs = GetConnectionString();

            }
            catch (System.ArgumentException ex)
            {

                _logger.Fatal($"Cannot get connection string: {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }
            catch (Exception ex)
            {

                _logger.Fatal($"Unable to access database : {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }

            MySqlConnection conn = new MySqlConnection(cs);

            PrincipalTable.DatabaseConnection = conn;

            if (!PrincipalTable.TableExists())
            {

                _logger.Fatal("Table does not exist, cannot add new quotes.");

                Console.WriteLine("Error: Operation aborted.");

                return;

            }

            string filename = null;

            filename = config["add"];

            if (null == filename)
            {

                _logger.Error("No quote file specified - " + 
                    "no quotes added.");

                Console.WriteLine(
                    "Error: Operation aborted - no file specified.");

                return;

            }

            _logger.Trace($"Adding quotes from file {filename}.");

            int count = PrincipalTable.AddQuotesFromFile(filename);

            if (0 >= count)
            {

                Console.WriteLine("No quotes added into database.");

            }
            else 
            {

                Console.WriteLine($"Number of quotes added = {count}.");

            }

            return;

        }

        // 
        // Output via the console a single quote given the quote ID
        // from the commandline argument "--qid".
        // 
        public static void GetQuoteByQid(IConfiguration config)
        {

            _logger.Trace("Retrieving quote...");

            string cs = null;

            try 
            {

                cs = GetConnectionString();

            }
            catch (System.ArgumentException ex)
            {

                _logger.Fatal($"Cannot get connection string: {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }
            catch (Exception ex)
            {

                _logger.Fatal($"Unable to access database : {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }

            MySqlConnection conn = new MySqlConnection(cs);

            PrincipalTable.DatabaseConnection = conn;

            if (!PrincipalTable.TableExists())
            {

                _logger.Fatal("Table does not already exist, " +
                    "cannot locate quotes by ID.");

                Console.WriteLine("Error: Operation aborted.");

                return;

            }

            string quoteId = config["qid"];

            string qid = config["qid"];

            Quote quote = null;

            if (null != qid) 
            {

                quote = PrincipalTable.GetQuoteByQid(qid);

            }

            if (null == quote)
            {

                string message = $"No quote with ID '{qid}' found.";

                _logger.Info(message);

                Console.WriteLine(message);

                return;

            }

            _logger.Info("Quote = " + quote.ToString());

            Console.WriteLine("-- Quote ID = {0}.", quote.ID);

            Console.WriteLine("-- Ext Ref ID = {0}.", quote.ExternalRefID);

            Console.WriteLine("-- Quote text = {0}.", quote.Content);

            Console.WriteLine("-- Quote source = {0}.", quote.Source);

            Console.WriteLine("-- Source URL = {0}.", quote.SourceUrl);

            Console.WriteLine("-- Quote entry datetime = {0}.", 
                quote.EntryDatetime);

            Console.WriteLine("-- Quote last mod = {0}.", 
                quote.ModTimestamp);

            Console.WriteLine("-- Quote = {0}.", quote.ToString());

        }

        //
        // Drops database table(s).
        // 
        public static void CleanUpDB(IConfiguration config)
        {

            _logger.Trace("Dropping database tables...");

            string cs = null;

            try 
            {

                cs = GetConnectionString();

            }
            catch (System.ArgumentException ex)
            {

                _logger.Fatal($"Cannot get connection string: {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }
            catch (Exception ex)
            {

                _logger.Fatal($"Unable to access database : {ex.Message}");

                Console.WriteLine($"Error: Operation aborted - {ex.Message}.");

                return;

            }

            MySqlConnection conn = new MySqlConnection(cs);

            PrincipalTable.DatabaseConnection = conn;

            if (false == PrincipalTable.TableExists())
            {

                 _logger.Info("Database table was not set up.  " + 
                     "No clean up needed.");

                 Console.WriteLine(
                     "No further actions required - table not setup.");

                 return;

            }


            _logger.Trace("Dropping database table...");

            PrincipalTable.DropTable();


            if (PrincipalTable.TableExists())

            {

                _logger.Error("Failed to clean up database!");

                Console.WriteLine("Error: Failed to clean up database!");

            }
            else 
            {

                _logger.Info("Database cleaned up");

                Console.WriteLine("Database cleaned up.");

            }

            return;

        }

        // 
        // Returns the database connection string based on the database
        // connection setting json file.
        // 
        // Function throws an exception if the connection string for 
        // "MySql" is not specified and the configuration file for
        // database connection string is not specified or inaccessible.
        //
        public static string GetConnectionString()
        {

            ConfigurationBuilder builder = null;

            IConfiguration config = null;

            string dbSettingsFile = Configuration["DBSettings"];

            _logger.Trace($"Config file for DB settings = {dbSettingsFile}.");

            if (null == dbSettingsFile) 
            {

                throw new ArgumentNullException(
                    "DBSettings", 
                    "Missing config file for database connection " + 
                    "string settings.");

            }

            builder = new ConfigurationBuilder();

            builder.SetBasePath(Directory.GetCurrentDirectory());

            builder.AddJsonFile(dbSettingsFile,
                optional: false, 
                reloadOnChange: true);

            config = builder.Build();

            IConfigurationSection dbConfig = config.GetSection("MySql");

            DBSettings mySqlSettings = new DBSettings();

            mySqlSettings.Server = dbConfig["Server"];

            _logger.Debug($"Server = {mySqlSettings.Server}.");

            mySqlSettings.Database = dbConfig["Database"];

            _logger.Debug($"Database = {mySqlSettings.Database}.");

            mySqlSettings.UserID = dbConfig["UserID"];

            mySqlSettings.UserPasswd = dbConfig["UserPasswd"];

            mySqlSettings.Pooling = dbConfig["Pooling"];

            _logger.Debug($"Pooling = {mySqlSettings.Pooling}.");

            string cs = mySqlSettings.ConnectionString;

            return cs;

        }

        // 
        // Load in the configuration file as specified by the command line.
        // 
        protected static void loadConsoleConfig()
        {

            string consoleConfigFile = Configuration["config"];

            _logger.Debug(
                $"Loading console configuration file {consoleConfigFile}.");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(consoleConfigFile, 
                    optional:false, reloadOnChange: true);

            var consoleConfig = builder.Build();

            int i = 0;

            foreach (var arg in consoleConfig.AsEnumerable())
            {

                string key = arg.Key;

                _logger.Trace("-- arg[{0}] = {1}, key = {2}.", 
                    ++i, arg, key);

                if (null != Configuration[key])
                {

                    _logger.Info($"Changing value for {key} from " + 
                            "'" + Configuration[key] + "' " + "to " + 
                            "'" + consoleConfig[key] + "' " + ".");
                        

                }

                Configuration[key] = consoleConfig[key];

            }

        }

        //
        // Parses the command line and determines if the program should
        // run in console mode for setup/debugging/cleanup purposes or 
        // it should run daemon mode to serve up the api.
        // 
        public static void Main(string[] args)
        {

            int argument_count = args.Length;

            LogConfig defaultLogConfig = new LogConfig(_logger);

            LoggingConfiguration defaultLoggingSetup = defaultLogConfig.SetUp();

            LogManager.Configuration = defaultLoggingSetup;

            if (!LogManager.IsLoggingEnabled())
            {

                LogManager.EnableLogging();

            }

            // 
            // Command line asking for help menu.
            // 
            if (0 < argument_count)
            {

                string targetLong = "--help";

                string targetShort = "-h";

                string[] matchesLong = 
                    Array.FindAll(args, s => s.Equals(targetLong));

                string[] matchesShort = 
                    Array.FindAll(args, s => s.Equals(targetShort));

                if ((0 < matchesLong.Length) || (0 < matchesShort.Length))
                {

                    CmdLineHelp();

                    return;

                }

            }

            // 
            // Get configuration from default configuration file.
            // 
            var builder = new ConfigurationBuilder();

            builder.SetBasePath(Directory.GetCurrentDirectory());

            builder.AddJsonFile(DefaultConfigFile,
                optional: false, 
                reloadOnChange: true);

            builder.AddEnvironmentVariables(prefix: "ASPNETCORE_");

            Configuration = builder.Build();

            _logger.Info($"Loaded config from {DefaultConfigFile}.");

            string nlogConfigFile = Configuration["NLog"];

            if (null == nlogConfigFile)
            {

                _logger.Debug("No configuration file specified for nlog " + 
                    "in app setting files, " +
                    "using default logging configuration.");

            }
            else 
            {

                NLog.LogManager.Configuration = 
                    new XmlLoggingConfiguration(nlogConfigFile, true);

                _logger.Info("Switching nlog configuration from default " + 
                    $"setting to {nlogConfigFile} as specified.");

            }

            // 
            // Get configuration from command line and overwrite defaults
            // where appropriate.
            // 
            var cmdLineBuilder = new ConfigurationBuilder();

            cmdLineBuilder.SetBasePath(Directory.GetCurrentDirectory());

            cmdLineBuilder.AddCommandLine(args, switchMappings);

            IConfiguration cmdLineConfig = cmdLineBuilder.Build();

            int i = 0;

            foreach (var arg in cmdLineConfig.AsEnumerable())
            {

                _logger.Trace("Command line arg[{0}] = {1}, key = {2}.", 
                    ++i, arg, arg.Key);

                if (!switchMappings.ContainsValue(arg.Key)) 
                {

                    string message = "Unknown option " + 
                        "'" + arg.Key + "' " +
                        "-- ignored.";

                    _logger.Warn(message);

                }
                else 
                {

                    string key = arg.Key;

                    Configuration[key] = arg.Value;

                    string message = $"Per command line, " + 
                        $"setting {key} to {Configuration[key]}."; 

                    _logger.Info(message);

                }

            }

            // 
            // Run the corresponding function based on mode and/or
            // command line argument.
            // 
            if (0 == argument_count) 
            {

                StartService();

            }
            else 
            {

                string configFile = Configuration["config"];

                if (null != configFile)
                {

                    try 
                    {

                        loadConsoleConfig();

                    }
                    catch (Exception ex)
                    {

                        _logger.Fatal(ex.Message);

                        _logger.Fatal("Abort operation.");

                        Console.WriteLine(
                            $"Error: Operation aborted - {ex.Message}.");

                        return;

                    }

                }

                string newLogConfigFile = Configuration["NLog"];

                if (null != newLogConfigFile)
                {

                    if (newLogConfigFile != nlogConfigFile) 
                    {

                        _logger.Trace("Switching logging configuration " +
                            $"to {newLogConfigFile}.");

                        nlogConfigFile = newLogConfigFile;

                        NLog.LogManager.Configuration = 
                            new XmlLoggingConfiguration(nlogConfigFile, 
                                true);

                        NLog.LogManager.Configuration = 
                            LogManager.Configuration.Reload();

                        NLog.LogManager.ReconfigExistingLoggers();

                        if (!LogManager.IsLoggingEnabled())
                        {

                            LogManager.EnableLogging();

                        }

                    }

                }

                i = 0;

                foreach (var arg in Configuration.AsEnumerable())
                {

                    _logger.Trace("-- arg[{0}] = {1}, key = {2}.", 
                        ++i, arg, arg.Key);

                }

                string qid = Configuration["qid"];

                string addQuotes = Configuration["add"];

                if (null != Configuration["mode"]) 
                {

                    string mode = Configuration["mode"];

                    _logger.Trace($"Mode = {mode}.");

                    switch (mode.ToLower()) 
                    {

                        case "api":

                            StartService();

                            break;

                        case "setup":

                            SetupAndSeed();

                            break;

                        case "cleanup":

                            CleanUpDB(Configuration);

                            break;

                        default:

                            string message = "Error: Invalid mode option " +
                                $"'{mode}'.";

                            _logger.Warn(message);

                            CmdLineHelp();

                            break;

                    }

                }

                if (null != qid)
                {

                    GetQuoteByQid(Configuration);

                }

                if (null != addQuotes)
                {

                    AddQuotesByFile(Configuration);

                }

            }

        }

    }

}

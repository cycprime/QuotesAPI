using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuotesAPI.Models;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;


namespace QuotesAPI
{
    public class Startup
    {

        // 
        // Logger for recording and displaying error messages.
        // 
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        // 
        // Configuration per application JSON file(s) that is loaded at
        // Startup().
        // 
        public IConfigurationRoot Configuration { get; }

        // 
        // Loads in the default configuration appsettings.json and any 
        // other files for various configuration settings for the 
        // application.  
        // 
        // If the default application configuration file appsettings.json
        // is missing, or if any of the other required configuration files
        // are missing, the function throws an exception.
        // 
        public Startup(IHostingEnvironment env)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{env.EnvironmentName}.json",
                    optional:
                    true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            string dbSettingsFile = Configuration["DBSettings"];

            if (null == dbSettingsFile)
            {

                throw new ArgumentNullException(
                    "DBSettings",
                    "Missing config file for database connection " +
                    "string settings.");

            }

            builder.AddJsonFile(dbSettingsFile, optional: false);

            Configuration = builder.Build();

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
            services.AddSwaggerGen();

            services.AddCors();

            // Set up service to access configured options.
            services.AddOptions();

            // Set up database configuration service.
            services.Configure<Database.Models.DBSettings>(
                Configuration.GetSection("MySql"));

            services.AddSingleton<IQuoteRepository, QuoteRepository>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            // add NLog to ASP.NET Core
            loggerFactory.AddNLog();

            string nlogConfig = Configuration["NLog"];

            if (null == nlogConfig)
            {

                _logger.Debug("WebAPI detects no nlog configuration file, " +
                    "using default logging configuration.");

                LogConfig defaultLogConfig = new LogConfig(_logger);

                LoggingConfiguration defaultLoggingSetup =
                    defaultLogConfig.SetUp();

                LogManager.Configuration = defaultLoggingSetup;

                if (!LogManager.IsLoggingEnabled())
                {

                    LogManager.EnableLogging();

                }

            }
            else
            {

                _logger.Info("WebAPI detected an nlog configuration file, " +
                    $"using {nlogConfig}.");

                env.ConfigureNLog(nlogConfig);

            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCors(builder => builder
                .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();

            // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
            app.UseSwaggerUi();




        }

    }

}

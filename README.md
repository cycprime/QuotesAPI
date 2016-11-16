# Welcome to QuotesAPI

QuotesAPI is a read only Web API that serves up random quotes.  

This is a test app that I have built to try out .NET Core.  It uses MySQL for its database, and NLog for logging.  

As the API was built prior to Oracle MySql supporting .NET Core, I made use of the [MySqlConnector](https://github.com/mysql-net/MySqlConnector) by bgrainger.  

I might update this later on.  For the time being, it is serviceable for the purpose of creating a web api for random quotes.


## Building
Install the latest [.NET Core](https://www.microsoft.com/net/core).

Clone directory:

    > git clone https://github.com/cycprime/QuotesAPI
    > cd QuotesAPI
    > dotnet restore

    
## Set Up
Before running the code, you would need to supply it with valid configurations.

To do so, create a directory called `Config`:

    > mkdir Config
    
For database connection credentials, copy `dbsettings.json` from `Sample` to `Config` and update the necessary credentials.

    > cp Sample/dbsettings.json Config

For NLog logging customization, copy `nlog.config` from `Sample` to `Config` and make customize the log output based on [NLog configuration](https://github.com/nlog/NLog/wiki/Configuration-file) specifications.

    > cp Sample/nlog.config Config
    
Make sure that the locations specified in `appsettings.json` for `"DBSettings"` and `"NLog"` are in synch with the locations of your customized configuration files.

Once the configurations are set up, compile the code:

    > dotnet build
    
Before your first time running the application as a web host serving up random quotes, you need to run the application in console mode to set up the database:

    > dotnet run --mode setup --seed <seed file>
    
A sample seed file, `SeedQuotes.json`, is available in the `Sample` directory.  You can modify it to include your own quotes to seed the database.

Assuming that the database has been seeded successfully, you can then run the application in web api mode:

    > dotnet run
    
To access a random quotes on your localhost, typein the URL:

    http://localhost:5000/api/RNGQuote/
    
It will return you a random quote from the database that you have just seeded.

To access the [Swagger](http://swagger.io/) documentation of the Random Quote API, type into the URL:

    http://localhost:5000/

This will allow you to try out different routes in the API.


## To Run This Particular App
*   To run in web api mode: 

	`> dotnet run`

*   Alternative way to run web api mode:

	`> dotnet run --mode api`

*   To set up database table and seed the database:

	`> dotnet run --mode setup --config <app config file> --seed <seed file>`

*   To clean up database tables:

	`> dotnet run --mode cleanup --config <app config file>`

*   To retrieve a quote by ID in console mode:

	`> dotnet run --config <app config file> --qid <quote ID>`

*   To add new quotes via console mode:

	`> dotnet run --config <app config file> --add <quote file>`


## App Config File Format
This is be the application configuration file.  By default, it uses the 
`appsettings.json` file.  

You can add more settings to the default `appsettings.json` file
by adding new JSON field-value pair or new section of field-value pairs.

For instance, to tell the application where the database configuration file
is located, simply add:

	  "DBSettings": "Config/dbsettings.json"

To specify a new NLog configuration file, simply add:

	  "NLog": "Config/nlog.config"

When running in console mode, you can specify an additional configuration
for the application via the `--config <app config file>` command line 
argument.

If the option specified in the additional configuration file are identical 
to the default `appsettings.json`, the setting in the new configuration file 
will take precedence.  

If the options specified in the new configuration file are not 
already present in the default `appsettings.json`, the application will 
add the new options to its list of configuration.

The `--config <app config file>` option is ignored when running in 
`--mode api` or in daemon mode.

## DB Config File Format
The database configuration file specifies the login details needed for
the application to access the MySQL database.  The settings should be in 
JSON file format.  

A sample file is available under `Sample/dbsettings.json`.  It should 
look similar to the following:

	{
	  "MySql": {
	    "Server": "localhost",
	    "Database": "csharp_test",
	    "UserID": "csharp_user",
	    "UserPasswd": "csharp_user_password",
	    "Pooling": "false"
	  }
	}

## Seed File Format
The seed file provides the initial list of quotes for populating the
newly created database table.

A sample file is available under `Sample/SeedQuotes.json`.  The file should 
contain elements similar to the following:

	[
	  {
	    "RefId": "<External Ref ID>",
	    "Content": "<Quote Content>",
	    "Source": "<Source of the Quote>",
	    "SourceUrl": "<URL for the Source>"
	  },
	  {
	    "RefId": "<External Ref ID>",
	    "Content": "<Quote Content>",
	    "Source": "<Source of the Quote>",
	    "SourceUrl": "<URL for the Source>"
	  }
	]

## Quote File Format
The quote file allows the insertion of additional quotes to the database.
The file has the same format as the seed file.

## Logging
By default, the application outputs its messages to the console.  Additionally,
it outputs error messages to `Logging/QuotesAPI.log`.

The application makes use of 
[NLog](https://github.com/nlog/NLog/wiki/Configuration-file).  As such, 
customization of the log configuration can be done via an XML file that 
complies with NLog configuration file format.  

A sample file of the NLog configuration is available at `Sample/nlog.config`.

Once the configuration is put in place, simply modify `appsettings.json` 
to point to the location of NLog configuration file.

## Docker
Published DLLs is required before building the image for Docker.  To publish the DLLs and all related files and directories for .NET Core, do the following:

    > dotnet publish
    
Once the DLLs and related files and directories have been published, run the normal build process for Docker to create the docker image for QuotesAPI:

    > docker build -t quotesapi .
    
The image will create a mount point at `/app/Logs` for log files, `/app/Data` for new quotes files (for seeding or new addition).  Use Docker's `--volume` option to map these mount points to the corresponding data volumes or local directories.

For more information about creating containers based on the QuotesAPI Docker image, please refer to the image, [cycprime/quotesapi](https://hub.docker.com/r/cycprime/quotesapi/), on DockerHub.

## License
This web api is licensed under the [MIT License](https://github.com/cycprime/QuotesAPI/blob/master/License.txt).

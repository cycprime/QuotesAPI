using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using QuotesAPI.Models;
using QuotesAPI.Util;
using NLog;


namespace QuotesAPI.Database.Models
{

    public static class PrincipalTable
    {

        // 
        // Attributes of the PrincipalTable class.
        // 
        private static readonly string _name = "quote_principal";

        private static readonly string _databaseType = "MySql";

        private static MySqlConnection _databaseConnection;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        // 
        // Accessor for the table name.
        //
        public static string Name
        {

            get
            {

                return _name;

            }

        }

        // 
        // Accessor for the type of database server the tables reside in.
        // 
        public static string DatabaseType
        {

            get
            {

                return _databaseType;

            }

        }

        // 
        // Accessor and setter for the database connection string
        // for the PrincipalTable.
        // 
        public static MySqlConnection DatabaseConnection
        {

            get 
            {

                return _databaseConnection;

            }

            set
            {

                _databaseConnection = value;

            }

        }

        //
        // Checks against the database to see if the Quotes table exist.
        // 
        // The function returns true if the table exists in the database.
        // The function returns false if the table does not exist or
        // if the function encounters error while accessing thee database.
        //
        // The funciton throws an exception if the database connection is
        // not initialized for the object.
        // 
        public static bool TableExists()
        {

            object result = null;

            bool returnValue = false;

            string table = Name;

            string query = 
                "select count(*) " +
                "from   information_schema.tables " +
                "where  table_schema = Database() " +
                "and    table_name = @table";

            logger.Trace($"query = {query}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                //
                // Just to be sure, start with a clean slate by closing out
                // any open/stale database connection.
                // 
                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        using (MySqlCommand cmd = 
                            new MySqlCommand(query, DatabaseConnection))
                        {

                            cmd.Parameters.Add("@table", DbType.String);

                            cmd.Parameters["@table"].Value = table;

                            logger.Trace("table = {0}.", table);

                            result = cmd.ExecuteScalar();

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;


                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            if (null != result) 
            {

                int count = Convert.ToInt32(result);

                if (1 == count)
                {

                    returnValue = true;

                    logger.Debug("TableExists() return value = true.");

                }

            }

            return returnValue;

        }

        // 
        // Returns quote details given a quote ID.
        //
        public static Quote GetQuoteByQid(string qid)
        {

            Quote quote = null;

            string table = Name;

            string query = "select	quote_id" + 
                ", ext_ref_id" + 
                ", quote_text" + 
                ", quote_source" + 
                ", quote_source_link" +
                ", quote_entry_datetime" + 
                ", quote_last_modified_datetime " +
                "from " + table + " " + 
                "where	quote_id = @quoteId ";

            logger.Trace($"query = {query}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);
                        // "Database connection has yet to be initialized.");

                }

                //
                // Just to be sure, start with a clean slate by closing out
                // any open/stale database connection.
                // 
                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    DbDataReader reader = null;

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = query;

                        cmd.Parameters.Add("@quoteId", DbType.String);

                        cmd.Parameters["@quoteId"].Value = qid;

                        cmd.Prepare();

                        reader = cmd.ExecuteReader();

                        logger.Trace("Reading result...");

                        while (reader.Read())
                        {

                            quote = new Quote();

                            quote.ID = reader.GetString(0);

                            quote.ExternalRefID = reader.GetString(1);

                            quote.Content = reader.GetString(2);

                            quote.Source = reader.GetString(3);

                            quote.SourceUrl = reader.GetString(4);

                            quote.EntryDatetime = reader.GetDateTime(5);

                            quote.ModTimestamp = reader.GetDateTime(6);

                            logger.Trace("quote = " + quote.ToString());

                        }

                        if ((null != reader) && !reader.IsClosed)
                        {

                            logger.Trace("Cleaning up data reader...");

                            reader.Dispose();

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            return quote;

        }

        // 
        // Returns a quote at a row located at the offset.
        // 
        // The quote entries are ordered by the row ID in the table.
        // An offset of 0 will return the first row.
        // 
        public static Quote GetQuoteAtRow(ulong offset)
        {

            if (0 > offset)
            {

                logger.Debug("Cannot find quote with " + 
                    "negative offset from the first row.");

                return null;

            }

            logger.Debug($"Get quote at offset {offset}.");

            Quote quote = null;

            string table = Name;

            string query = "select	quote_id" + 
                ", ext_ref_id" + 
                ", quote_text" + 
                ", quote_source" + 
                ", quote_source_link" +
                ", quote_entry_datetime" + 
                ", quote_last_modified_datetime " +
                "from " + table + " " + 
                "order by id " + 
                "limit  @offset" + 
                ", 1";

            logger.Trace($"query = {query}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    DbDataReader reader = null;

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = query;

                        cmd.Parameters.Add("@offset", DbType.UInt64);

                        cmd.Parameters["@offset"].Value = offset;

                        cmd.Prepare();

                        reader = cmd.ExecuteReader();

                        logger.Trace("Reading result...");

                        while (reader.Read())
                        {

                            quote = new Quote();

                            quote.ID = reader.GetString(0);

                            quote.ExternalRefID = reader.GetString(1);

                            quote.Content = reader.GetString(2);

                            quote.Source = reader.GetString(3);

                            quote.SourceUrl = reader.GetString(4);

                            quote.EntryDatetime = reader.GetDateTime(5);

                            quote.ModTimestamp = reader.GetDateTime(6);

                            logger.Trace("quote = " + quote.ToString());

                        }

                        if ((null != reader) && !reader.IsClosed)
                        {

                            logger.Trace("Cleaning up data reader...");

                            reader.Dispose();

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            return quote;

        }

        // 
        // Returns a number of quotes between the beginning offset and the
        // ending entry.
        // 
        // For instance, a start of 1 and an end of 12 would retrieve 
        // entries 1 through 12 inclusively.
        // 
        // The function returns the list of quote entries as a list.
        // 
        // The function throws an exception if the starting point is less 1,
        // or if the ending entry is prior to the starting entry.
        // 
        // It also throws an exception if the database connectionn is not 
        // configured.
        // 
        public static IEnumerable<Quote> GetQuotesInRange(ulong start, 
            ulong end)
        {

            if (1 > start) 
            {

                string message = "Start index cannot be less than 0.";

                logger.Error(message);

                throw new ArgumentOutOfRangeException("start", message);

            }

            logger.Debug($"Start index = {start}.");

            if (end < start)
            {

                string message = "The ending index needs to be " + 
                    "greater than or equal to he starting index.";

                logger.Error(message);

                throw new ArgumentOutOfRangeException("end", message);

            }

            logger.Debug($"Ending index = {end}.");

            List<Quote> quotes = new List<Quote>();

            string table = Name;

            ulong rows = (end - start) + 1;

            ulong offset = start - 1;

            string query = "select	quote_id" + 
                ", ext_ref_id" + 
                ", quote_text" + 
                ", quote_source" + 
                ", quote_source_link" +
                ", quote_entry_datetime" + 
                ", quote_last_modified_datetime " +
                "from " + table + " " + 
                "order by quote_entry_datetime,  quote_source, id " +
                "limit  @offset" + 
                ", @numberOfRows";

            logger.Trace($"query = {query}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    DbDataReader reader = null;

                    try {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = query;

                        cmd.Parameters.Add("@offset", DbType.UInt64);

                        cmd.Parameters["@offset"].Value = offset;

                        cmd.Parameters.Add("@numberOfRows", DbType.UInt64);

                        cmd.Parameters["@numberOfRows"].Value = rows;

                        cmd.Prepare();

                        reader = cmd.ExecuteReader();

                        logger.Trace("Reading result...");

                        while (reader.Read())
                        {

                            Quote quote = new Quote();

                            quote.ID = reader.GetString(0);

                            quote.ExternalRefID = reader.GetString(1);

                            quote.Content = reader.GetString(2);

                            quote.Source = reader.GetString(3);

                            quote.SourceUrl = reader.GetString(4);

                            quote.EntryDatetime = reader.GetDateTime(5);

                            quote.ModTimestamp = reader.GetDateTime(6);

                            logger.Trace("quote = " + quote.ToString());

                            quotes.Add(quote);

                        }

                        if ((null != reader) && !reader.IsClosed)
                        {

                            logger.Trace("Cleaning up data reader...");

                            reader.Dispose();

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            return quotes;

        }

        //
        // Returns the number of entries in the principal quote table.
        // 
        public static ulong QuotesCount()
        {

            ulong dbQuoteCount = 0L;

            string table = Name;

            string query = "select  count(*) " +
                "from   " + table;

            logger.Trace($"query = {query}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string message = 
                        "Database connection has yet to be initialized.";

                    logger.Error(message);

                    throw new System.ArgumentException(message);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        using (MySqlCommand cmd = 
                            new MySqlCommand(query, DatabaseConnection))
                        {

                            cmd.Prepare();

                            object result;

                            result = cmd.ExecuteScalar();

                            if (null != result)
                            {

                                dbQuoteCount = Convert.ToUInt64(result);

                                logger.Trace($"Quote count = {dbQuoteCount}.");

                            }

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        return dbQuoteCount;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            return dbQuoteCount;

        }

        // 
        // Returns a random quote located off of an offset determined by a 
        // number generated by the random number generator. 
        // 
        // If no quote is found, the function will repeat the attempt 3 times.
        // This is to take into account situation where entries in the database
        // might have deleted between the time the total row count is taken
        // and when a quote is retrieved.
        // 
        public static Quote RandomQuote()
        {

            if (null == DatabaseConnection) 
            {

                string errorMsg = 
                    "Database connection has yet to be initialized.";

                logger.Error(errorMsg);

                throw new System.ArgumentException(errorMsg);

            }

            Quote quote = null;

            int trials = 3;

            while ((null == quote) && (0 < trials))
            {

                ulong count = QuotesCount();

                logger.Debug($"Total number of quotes = {count}.");

                if (0 >= count)
                {

                    logger.Error("Failed to retrieve a random quote - " + 
                        "total number of quotes cannot be negative.");

                    return null;

                }

                logger.Trace("Retrieving quote from random offset...");

                ulong offset = ULRandom.NextULong(0, count);

                quote = GetQuoteAtRow(offset);

                --trials;

            }

            return quote;

        }

        // 
        // Create the principal quote table (including constraints, indices)
        // in the database. 
        // 
        // The function throws an exception if the database connection
        // has not been initialized for the object.
        // 
        public static void CreateTable()
        {

            string table = Name;
                    
            string createQuery = "create table if not exists " +
                Name + 
                @"(
	id	bigint unsigned	not null auto_increment	primary	key
		comment ""Database generated unique entry identifier.""
    , quote_id	varchar(128)	not null	unique
		comment ""A unique quote identifier.""
    , ext_ref_id	varchar(256)	null	default """"
		comment ""An external ID for cross referencing purposes, particularly in bulk input.""
    , quote_text	text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci	not null
		comment	""The text of the quote.""
	, quote_source	varchar(4096)	not null
		comment	""A free-text description of where the quote comes from.""
	, quote_source_link	varchar(256)
		comment ""URL for the source.""
	, quote_entry_datetime datetime	not null	default current_timestamp
 		comment ""Date and timestamp at which the quote was entered into the table.""
	, quote_last_modified_datetime timestamp	not null	default current_timestamp	on update current_timestamp
		comment ""Date and timestamp at which the entry was last modified in the table.""
) comment=""Table containing quotes from various sources.""
                ";

            logger.Trace($"Create table query = {createQuery}.");

            string constraintQuery = "alter table " + 
                Name + 
                @"
	add constraint	ak_quote_principal_01	unique key	(quote_id)
    	comment	""Mutable natural key, can be referenced by other tables.""
                ";

            logger.Trace($"Create constraint query = {constraintQuery}.");

            string indexQuery = "alter table " + 
                Name + 
                @"
    add index	idx_quote_principal_01	(quote_entry_datetime)
    , add index	idx_quote_principal_02	(quote_last_modified_datetime)
    , add index	idx_quote_principal_03	(ext_ref_id)
                ";

            logger.Trace($"Create index query = {indexQuery}.");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = createQuery;

                        cmd.ExecuteNonQuery();

                        // Console.WriteLine("Executed query: {0}.", createQuery);

                        cmd.CommandText = constraintQuery;

                        cmd.ExecuteNonQuery();

                        // Console.WriteLine("Executed query: {0}.", constraintQuery);

                        cmd.CommandText = indexQuery;

                        cmd.ExecuteNonQuery();

                        // Console.WriteLine("Executed query: {0}.", indexQuery);

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

        }

        // 
        // Add triggers to the principal quote table for creating the 
        // unique quote ID and for recording the quote entry time.
        // 
        // The function throws an exception if the database connection
        // has yet to be initialized.
        // 
        public static void AddTableTriggers()
        {

            string table = Name;

            string triggerName = "tr_" + Name + "_01";
                    
            string dropTriggerQuery = 
                "drop trigger if exists " + triggerName;

            logger.Trace($"Drop trigger query = {dropTriggerQuery}.");

            string createTriggerQuery = @"
-- delimiter #
create trigger	" + 
    triggerName 
    + @"
	before insert on " + 
    Name + 
    @"
    for each row
    begin
		declare tbl_name varchar(64) default '" + Name + @"';
        declare	new_id_value bigint unsigned;
		declare	new_quote_id_string varchar(256);
		declare creation_dt timestamp;
		declare database_name varchar(64)	default	database();
        
        set new_id_value = (
			SELECT AUTO_INCREMENT 
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA=DATABASE()
            AND TABLE_NAME= tbl_name
        );
        
        set creation_dt = current_timestamp;
       
		set new_quote_id_string = concat(
			'Quote_00_'
            , cast(new_id_value as char(20))
            , '_'
            , cast(creation_dt as char(19))
            , '_'
            , cast(database_name as char(64))
            , '_'
            -- , cast(text_length as char(10))
            , substring(new.quote_text, 1, 100)
            , '_00'
		);

		set NEW.quote_id = (
            conv(substring(cast(sha(new_quote_id_string) as char), 1, 16), 16, 10)
		);
        
        set NEW.quote_entry_datetime = creation_dt;

    end;#
    
-- delimiter
                ";


            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = dropTriggerQuery ;

                        cmd.ExecuteNonQuery();

                        logger.Trace("Executed query to drop triggeer: " + 
                            $"{dropTriggerQuery }");

                        cmd.CommandText = createTriggerQuery;

                        cmd.ExecuteNonQuery();

                        logger.Trace("Executed query to create triggeer: " + 
                            $"{createTriggerQuery}");

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

        }

        // 
        // Drop the principal quote database table.
        // 
        // The function throws an exception if the database connection
        // for the object has not been initialized.
        // 
        public static void DropTable()
        {

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        string table = Name;

                        string dropQuery = "drop table " + Name;

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = dropQuery;

                        cmd.ExecuteNonQuery();

                        logger.Trace("Executed query to drop table: " + 
                            $"{dropQuery}");

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        throw ex;

                    }
                    finally
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

        }

        // 
        // Insert a quote into the principal quote table.
        // 
        // The function returns true on successfully inserting a quote
        // entry into the database and false otherwise.
        // 
        // The function expects the quote text and the quote's source to be 
        // non-empty strings.  If an empty string is detected in either field,
        // the function will not insert the quote into the database, even 
        // if other fields are non-empty.
        // 
        // The function throws an exception if the database connection 
        // for the object is not initialized.
        //
        public static bool AddQuote(QuoteInput quoteInput)
        {

            bool inserted = false;

            if (null == quoteInput.Content) 
            {

                logger.Error("Quote is empty, insertion failed.");

                return inserted;

            }

            string table = Name;

            string insertQuery = 
                "insert into " + table + " " + 
                "(" + 
                    "quote_id" + 
                    ", ext_ref_id" + 
                    ", quote_text" + 
                    ", quote_source" + 
                    ", quote_source_link" + 
                ") " + 
                "values	(" + 
                    "@quoteId" + 
                    ", @refId" + 
                    ", @quoteText" + 
                    ", @quoteSource" + 
                    ", @sourceUrl" + 
                ") ";

            logger.Trace($"Quote insertion query = {insertQuery}");

            lock(DatabaseConnection)
            {

                if (null == DatabaseConnection) 
                {

                    string errorMsg = 
                        "Database connection has yet to be initialized.";

                    logger.Error(errorMsg);

                    throw new System.ArgumentException(errorMsg);

                }

                if (DatabaseConnection.State != ConnectionState.Closed)
                {

                    logger.Trace("Closing connection that was left opened...");

                    DatabaseConnection.Close();

                }

                if (DatabaseConnection.State != ConnectionState.Open)
                {

                    try 
                    {

                        logger.Trace("Opening DB connection...");

                        DatabaseConnection.Open();

                        MySqlCommand cmd = new MySqlCommand();

                        cmd.Connection = DatabaseConnection;

                        cmd.CommandText = insertQuery;

                        cmd.Parameters.Add("@quoteId", DbType.String);

                        cmd.Parameters.Add("@refId", DbType.String);

                        cmd.Parameters.Add("@quoteText", DbType.String);

                        cmd.Parameters.Add("@quoteSource", DbType.String);

                        cmd.Parameters.Add("@sourceUrl", DbType.String);

                        cmd.Parameters["@quoteId"].Value = "";

                        cmd.Parameters["@refId"].Value = "";

                        if (null != quoteInput.RefId) 
                        {

                            cmd.Parameters["@refId"].Value = quoteInput.RefId;

                        }

                        cmd.Parameters["@quoteText"].Value = quoteInput.Content;

                        cmd.Parameters["@quoteSource"].Value = quoteInput.Source;

                        cmd.Parameters["@sourceUrl"].Value = "";

                        if (null != quoteInput.SourceUrl)
                        {

                            cmd.Parameters["@sourceUrl"].Value = 
                                quoteInput.SourceUrl;

                        }

                        cmd.Prepare();

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (1 == rowsAffected)
                        {

                            inserted = true;

                            logger.Trace("Quote successfully inserted.");

                        }

                    }
                    catch (MySqlException ex)
                    {

                        logger.Error("Database error: {0}.", ex.ToString());

                        logger.Error("Quote insertion has failed.");

                        return inserted;

                    }
                    finally 
                    {

                        if (ConnectionState.Open == DatabaseConnection.State)
                        {

                            logger.Trace("Closing DB connection...");

                            DatabaseConnection.Close();

                        }

                    }

                }

            }

            return inserted;

        }

        // 
        // Read one or more json quote block from a file and insert
        // quotes into the principal database tbale.
        // 
        // The function returns the number of quotes inserted into the
        // database.
        // 
        // The function expects the file to contain json representation
        // of quotes:
        // [
        //  {
        //    "RefId": "<External Ref ID>",
        //    "Content": "<Quote Content>",
        //    "Source": "<Source of the Quote>",
        //    "SourceUrl": "<URL for the Source>"
        //   }
        // ]
        //
        // The function throws an exception if the file supplied is null.
        //
        public static int AddQuotesFromFile(string file)
        {

            if (null == file)
            {

                string errorMsg = "Missing filename for adding quotes.";

                logger.Error(errorMsg);

                throw new System.ArgumentException(errorMsg, file);

            }

            List<QuoteInput> seedList = null;

            try 
            {

                logger.Trace("Opening file and loading content to stream...");

                using (StreamReader fStream = File.OpenText(file))
                {

                    string jsonText = fStream.ReadToEnd();

                    seedList = JsonConvert.DeserializeObject<List<QuoteInput>>(
                        jsonText);

                }

            }
            catch (OutOfMemoryException ex)
            {

                logger.Error("Out of memory when reading file: " + 
                        ex.Message);

                logger.Error("Quotes not inserted into the table.");

                return 0;

            }
            catch (Exception ex)
            {

                logger.Error("Encountered error while reading file: " + 
                        ex.Message);

                logger.Error("No quotes added to table.");

                return 0;

            }

            int insertionCount = 0;

            for (int i = 0; i < seedList.Count(); ++i)
            {

                QuoteInput seed = seedList[i];

                if (true == AddQuote(seed))
                {

                    ++insertionCount;

                }

            }

            logger.Info(insertionCount + 
                " quotes have been added to the database."
            );

            return insertionCount;

        }

    }

}

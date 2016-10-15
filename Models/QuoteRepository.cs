using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using QuotesAPI.Database.Models;
using QuotesAPI.Util;

namespace QuotesAPI.Models
{

    public class QuoteRepository:IQuoteRepository
    {

        //
        // A maximum number of entries allowed per page.
        //
        public const uint MaxEntriesPerPage = 1000;

        //
        // A default number of quotes per page.
        //
        public const uint DefaultEntriesPerPage = 10;

        // 
        // A list of quotes referenced by their quote ID.
        // 
        private static ConcurrentDictionary<string, Quote> _quotes = 
            new ConcurrentDictionary<string, Quote>();

        // 
        // Database settings per configuration.
        // 
        private readonly DBSettings _dbSettings;

        //
        // MySQL database connection.
        // 
        MySqlConnection _dbConn = null;


        // 
        // Constructor
        // 
        public QuoteRepository(IOptions<DBSettings> dbSettings)
        {

            _dbSettings = dbSettings.Value;

            configDbConn();

        }

        // 
        // Returns a quote given a quote ID.
        // 
        public Quote Find(string quoteId)
        {

            if (null == _dbConn)
            {

                return null;

            }

            Quote quote = null;

            if (PrincipalTable.TableExists())
            {

                quote = PrincipalTable.GetQuoteByQid(quoteId);

            }

            return quote;

        }

        //
        // Returns a random quote based on the internal random number generator.
        // 
        public Quote Random()
        {

            Quote quote = null;

            if (PrincipalTable.TableExists())
            {

                quote = PrincipalTable.RandomQuote();

            }

            return quote;

        }

        //
        // Returns a list of quotes given a page number.
        //
        // By default, the function retrieves 10 quotes at a time.  The
        // caller can specify which "lot" of ten it would like to retrieve.
        //
        // The functiont throws exception if the page number specified is
        // less than or equal to 0, or if it is outside the total number
        // of pages given the numebr of quotes.
        // 
        // It also throws an exception or if the page size specified is larger
        // than the max number of entries allowed, or if the 
        // 
        public IEnumerable<Quote> QuotesByPage(
            ulong pageNum, 
            uint pageSize = 10)
        {

            if (0 >= pageNum)
            {

                throw new ArgumentOutOfRangeException(
                    "pageNum",
                    "Page number cannot be less than 1."
                );

            }

            if ((0 >= pageSize) || (MaxEntriesPerPage < pageSize))
            {

                throw new ArgumentOutOfRangeException(
                    "pageSize",
                    "Number of entries per page is outside " + 
                    "permissible range.");

            }

//             ulong quoteCount = Count();
// 
//             ulong maxNumOfPages = (quoteCount + pageSize) / pageSize;

            ulong maxNumOfPages = PageCount(pageSize);

            if (pageNum > maxNumOfPages)
            {

                throw new ArgumentOutOfRangeException(
                    "pageNum",
                    "Page number specified exceeds max number of pages."
                );

            }

            ulong beginningRow = (pageNum - 1) * pageSize + 1;

            ulong endRow = (beginningRow - 1) + pageSize;

            IEnumerable<Quote> quotes = null;

            if (PrincipalTable.TableExists())
            {

                Console.WriteLine("Retrieving entry {0} to {1}...", 
                    beginningRow, 
                    endRow);

                quotes = PrincipalTable.GetQuotesInRange(beginningRow, endRow);

            }

            return quotes;

        }

        // 
        // Returns the number of pages given the number of quotes
        // per page.
        //
        // The function throws an exception if the number of quotes
        // per page is less than or equal to 0.
        // 
        public ulong PageCount(uint pageSize = DefaultEntriesPerPage)
        {

            if (0 >= pageSize)
            {

                throw new ArgumentOutOfRangeException(
                    "pageSize",
                    "Number of entries per page cannot be less than or " +
                    "equal to zero.");

            }

            ulong quoteCount = Count();

            ulong maxNumOfPages = (quoteCount + pageSize) / pageSize;

            ulong remindar = quoteCount % pageSize;

            if ((0 == remindar) && (0 < maxNumOfPages)) 
            {

                --maxNumOfPages;

            }

            return maxNumOfPages;

        }

        // 
        // Returns the number of quotes in the database.
        // 
        public ulong Count()
        {

            ulong quoteCount = 0L;

            if (null == _dbConn)
            {

                return 0L;

            }

            if (PrincipalTable.TableExists())
            {

                quoteCount = PrincipalTable.QuotesCount();

            }

            return quoteCount;

        }

        // 
        // Initializes the database connection for access to the principal
        // quote table.
        // 
        protected void configDbConn()
        {

            string connectionString = _dbSettings.ConnectionString;

            _dbConn = new MySqlConnection(connectionString);

            PrincipalTable.DatabaseConnection = _dbConn;

        }

    }

}

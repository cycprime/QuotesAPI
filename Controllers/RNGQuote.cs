using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QuotesAPI.Database.Models;
using QuotesAPI.Models;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QuotesAPI.Controllers
{
    [Route("api/[controller]")]
    public class RNGQuote : Controller
    {

        private IQuoteRepository _quoteRepo;

        private static ILogger<RNGQuote> _logger;

        public RNGQuote(IQuoteRepository quotes, ILogger<RNGQuote> logger)
        {

            _quoteRepo = quotes;

            _logger = logger;

        }

        // 
        // Default number of quotes per page.
        // 
        public const uint DefaultEntriesPerPage = 10;


        // GET api/RNGQuotes
        [HttpGet]
        // public IEnumerable<string> Get()
        public IActionResult Get()
        {

            _logger.LogTrace("Retrieving a random quote...");

            Quote quote = null;

            quote = _quoteRepo.Random();

            if (null == quote)
            {

                _logger.LogError("Not found - " + 
                    "No random quote returned from Random().");

                return NotFound();

            }

            return new ObjectResult(quote);

        }

        // GET api/RNGQuotes/1860116338409161551
        //
        // The route returns the quote if a quote with the specified 
        // Quote ID is found in the database.
        // 
        // The function returns 404 if no quote is not found.
        // 
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {

            _logger.LogTrace($"Retrieving quote with ID = {id}...");

            Quote quote = null;

            quote = _quoteRepo.Find(id);

            if (null == quote)
            {

                _logger.LogDebug($"Quote id {id} not found.");

                return NotFound();

            }

            return new ObjectResult(quote);

        }

        // GET api/RNGQuotes/all/<page_number>?entries=<entries_per_page>
        //
        // Returns all the quotes one page at a time.
        // 
        // Caller can specify the number of entries per page.  If number
        // of entries per page is not specified, the function assumes a 
        // default value.
        // 
        [HttpGet("all/{num}")]
        public IActionResult GetPage(ulong num, 
            [FromQuery]uint entries = DefaultEntriesPerPage)
        {

            _logger.LogTrace($"Retrieving page {num}, " + 
                $"with {entries} per page...");

            IEnumerable<Quote> quotes = null;

            if ((0 >= num) || (0 >= entries))
            {

                _logger.LogDebug($"Bad request, with page num = {num}, " + 
                    "entries = {entries}.");

                return BadRequest();

            }

            try 
            {

                quotes = _quoteRepo.QuotesByPage(num, entries);

            }
            catch (ArgumentOutOfRangeException ex)
            {
                
                _logger.LogDebug("Bad request - " + ex.ToString());


                return BadRequest();

            }

            return new ObjectResult(quotes);

        }

        // GET api/RNGQuotes/all/PageCount?entries=<entries_per_page>
        // 
        // Returns the number of pages the quotes would fit into 
        // given the number of entries allowed per page.
        // 
        // If the number of entries per page is not specified, the function
        // assumes a default value.
        //
        [HttpGet("all/PageCount")]
        public IActionResult PageCount([FromQuery]uint entries = 
            DefaultEntriesPerPage)
        {

            _logger.LogTrace($"Getting page count with {entries} per page...");

            if (0 >= entries)
            {

                _logger.LogDebug("Bad request - " + 
                    "entries per page requested is negative.");

                return BadRequest();

            }

            ulong numberOfPages = 0L;

            try 
            {

                numberOfPages = _quoteRepo.PageCount(entries);

            }
            catch (ArgumentOutOfRangeException ex)
            {

                _logger.LogDebug("Bad request - " + ex.ToString());

                return BadRequest();

            }

            return new ObjectResult(numberOfPages);

        }

        // 
        // Returns the total number of quotes in the system.
        // 
        [HttpGet("QuotesCount")]
        public IActionResult Count()
        {

            _logger.LogTrace("Getting total number of quotes...");

            ulong count = 0L;

            count = _quoteRepo.Count();

            _logger.LogTrace($"Quote count = {count}.");

            return new ObjectResult(count);

        }

//         // POST api/values
//         [HttpPost]
//         public void Post([FromBody]string value)
//         {
//         }
// 
//         // PUT api/values/5
//         [HttpPut("{id}")]
//         public void Put(int id, [FromBody]string value)
//         {
//         }
// 
//         // DELETE api/values/5
//         [HttpDelete("{id}")]
//         public void Delete(int id)
//         {
//         }

    }

}

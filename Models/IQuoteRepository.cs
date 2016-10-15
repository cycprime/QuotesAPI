using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuotesAPI.Database.Models;

namespace QuotesAPI.Models
{
    public interface IQuoteRepository
    {

        Quote Find(string QuoteId);

        Quote Random();

        IEnumerable<Quote> QuotesByPage(ulong pageNum, uint pageSize = 10);

        ulong PageCount(uint pageSize = 10);

        ulong Count();

    }
}

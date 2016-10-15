using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuotesAPI.Models
{

    public class QuoteInput
    {

        public string RefId {get; set;}

        public string Content {get; set;}

        public string Source {get; set;}

        public string SourceUrl {get; set;}

    }

}

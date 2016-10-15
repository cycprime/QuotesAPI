using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuotesAPI.Database.Models
{

    public class Quote
    {

        string _quoteId;

        string _extRefId;

        string _quoteText;

        string _quoteSource;

        string _sourceURL;

        DateTime _entryDatetime;

        DateTime _modDatetime;


        public string ID 
        {

            get 
            {

                return _quoteId;

            }
            
            set
            {
                _quoteId = value;

            }

        }

        public string ExternalRefID
        {

            get
            {

                return _extRefId;

            }

            set
            {

                _extRefId = value;

            }

        }

        public string Content 
        {

            get
            {

                return _quoteText;

            }

            set
            {

                _quoteText = value;

            }

        }

        public string Source 
        {

            get
            {

                return _quoteSource;

            }
            
            set
            {

                _quoteSource = value;

            }
        }

        public string SourceUrl
        {

            get
            {

                return _sourceURL;

            }

            set
            {

                _sourceURL = Uri.EscapeUriString(value);

            }

        }

        public DateTime EntryDatetime 
        {

            get
            {

                return _entryDatetime;

            }

            set
            {

                _entryDatetime = value;

            }

        }

        public DateTime ModTimestamp 
        {

            get
            {

                return _modDatetime;

            }

            set
            {

                _modDatetime = value;

            }

        }


        // 
        // Concatenates the ID, the quote text, the source, and the source 
        // URL into a string.
        //
        // The output string would look something to:
        //  <ID>: "<quote text>" --[quote source](quote_url) 
        // 
        public override string ToString()
        {

            string output = "";

            output = "" + ID + ":" + " " +
                "\"" + Content + "\" " + 
                " --" + "[" + Source + "]" + 
                "(" + SourceUrl + ")";

            return output;

        }

    }

}

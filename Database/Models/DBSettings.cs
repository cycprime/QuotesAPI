namespace QuotesAPI.Database.Models
{
    public class DBSettings
    {

        public string Server {get; set;}

        public string Database {get; set;}

        public string UserID {get; set;}

        public string UserPasswd {get; set;}

        public string Pooling {get; set;}

        public string ConnectionString
        {

            get
            {

                string cs = "Server=" + Server + 
                    ";Database=" + Database + 
                    ";User ID=" + UserID + 
                    ";Password=" + UserPasswd +
                    ";Pooling=" + Pooling;

                return cs;

            }

        }

    }
}

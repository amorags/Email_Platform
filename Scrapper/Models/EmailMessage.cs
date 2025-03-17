using System;

namespace EmailCleanerAPI
{
    public class EmailMessage
    {
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public DateTime Date { get; set; }
        public string Body { get; set; }
    }
}

using Azure;
using Azure.Data.Tables;

namespace Weather_API
{
    public class Email : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string EmailAddress { get; set; }

        public Email(string email)
        {
            string[] emailParts = email.Split("@");
            PartitionKey = emailParts[1];
            RowKey = emailParts[0];
            ETag = new ETag(email);
            EmailAddress = email;
        }

        public Email() { }
    }
}

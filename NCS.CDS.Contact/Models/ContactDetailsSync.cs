namespace NCS.DSS.Contact.Models
{
    public class ContactDetailsSync
    {
        public Guid CustomerId { get; set; }
        public string MobileNumber { get; set; }
        public string HomeNumber { get; set; }
        public string AlternativeNumber { get; set; }
        public string EmailAddress { get; set; }
    }
}

﻿namespace NCS.DSS.Contact.Models
{
    public class DigitalIdentity
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public Guid? IdentityID { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? IdentityStoreId { get; set; }
        public string LegacyIdentity { get; set; }
        public string id_token { get; set; }
        public DateTime? LastLoggedInDateTime { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string LastModifiedTouchpointId { get; set; }
        public DateTime? DateOfTermination { get; set; }
        public string CreatedBy { get; set; }
    }
}

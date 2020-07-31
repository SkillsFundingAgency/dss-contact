using System;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Models
{
    public interface IContactDetails
    {
        PreferredContactMethod? PreferredContactMethod { get; set; }
        string MobileNumber { get; set; }
        string HomeNumber { get; set; }
        string AlternativeNumber { get; set; }
        string EmailAddress { get; set; }
        DateTime? LastModifiedDate { get; set; }
        string LastModifiedTouchpointId { get; set; }
        void SetDefaultValues();

    }
}
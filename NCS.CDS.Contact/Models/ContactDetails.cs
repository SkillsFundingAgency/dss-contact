using System;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Models
{
    public class ContactDetails : IContactDetails
    {
        [Display(Description = "Unique identifier for a contact record")]
        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public Guid? ContactId { get; set; }
     
        [Display(Description = "Unique identifier of a customer")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? CustomerId { get; set; }

        [Display(Description = "Customers preferred contact method" +
                                "1 - Email,   " +
                                "2 - Mobile,   " +
                                "3 - Telephone,   " +
                                "4 - SMS,   " +
                                "5 - Post,   " +
                                "99 - Not known")]
        [Example(Description = "3")]
        public PreferredContactMethod? PreferredContactMethod { get; set; }

        [StringLength(20)]
        [Example(Description = "0777 435 635")]
        public string MobileNumber { get; set; }

        [StringLength(20)]
        [Example(Description = "0121 888777")]
        public string HomeNumber { get; set; }

        [StringLength(20)]
        [Example(Description = "0121 444889")]
        public string AlternativeNumber { get; set; }

        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$")]
        [StringLength(255)]
        [Example(Description = "user@organisation.com")]
        public string EmailAddress { get; set; }

        [Example(Description = "2018-06-21T17:45:00")]
        public DateTime? LastModifiedDate { get; set; }

        [StringLength(10, MinimumLength = 10)]
        [Display(Description = "Identifier of the touchpoint who made the last change to the record")]
        [Example(Description = "0000000001")]
        public string LastModifiedTouchpointId { get; set; }

        public void SetDefaultValues()
        {
            ContactId = Guid.NewGuid();

            if (!LastModifiedDate.HasValue)
                LastModifiedDate = DateTime.UtcNow;

            if (PreferredContactMethod == null)
                PreferredContactMethod = ReferenceData.PreferredContactMethod.NotKnown;
        }

        public void SetIds(Guid customerId, string touchpointId)
        {
            ContactId = Guid.NewGuid();
            CustomerId = customerId;
            LastModifiedTouchpointId = touchpointId;
        }

        public void Patch(ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetailsPatch == null)
                return;

            if(contactdetailsPatch.PreferredContactMethod.HasValue)
                PreferredContactMethod = contactdetailsPatch.PreferredContactMethod;

            if(!string.IsNullOrEmpty(contactdetailsPatch.AlternativeNumber))
                AlternativeNumber = contactdetailsPatch.AlternativeNumber;

            if(!string.IsNullOrEmpty(contactdetailsPatch.EmailAddress))
                EmailAddress = contactdetailsPatch.EmailAddress;

            if (!string.IsNullOrEmpty(contactdetailsPatch.HomeNumber))
                HomeNumber = contactdetailsPatch.HomeNumber;

            if (!string.IsNullOrEmpty(contactdetailsPatch.MobileNumber))
                MobileNumber = contactdetailsPatch.MobileNumber;

            if(contactdetailsPatch.LastModifiedDate.HasValue)
                LastModifiedDate = contactdetailsPatch.LastModifiedDate;

            if (!string.IsNullOrEmpty(contactdetailsPatch.LastModifiedTouchpointId))
                LastModifiedTouchpointId = contactdetailsPatch.LastModifiedTouchpointId;

        }
    }    
}
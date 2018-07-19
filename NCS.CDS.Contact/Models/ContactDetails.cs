using System;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Models
{
    public class ContactDetails
    {
        [Display(Description = "Unique identifier for a contact record")]
        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public Guid? ContactID { get; set; }
     
        [Display(Description = "Unique identifier of a customer")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? CustomerId { get; set; }

        [Example(Description = "3")]
        public PreferredContactMethod? PreferredContactMethodID { get; set; }

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

        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
        public Guid? LastModifiedTouchpointID { get; set; }


        public void Patch(ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetailsPatch == null)
                return;

            if(contactdetailsPatch.PreferredContactMethodID.HasValue)
                PreferredContactMethodID = contactdetailsPatch.PreferredContactMethodID;

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

            if (contactdetailsPatch.LastModifiedTouchpointID.HasValue)
                LastModifiedTouchpointID = contactdetailsPatch.LastModifiedTouchpointID;

        }
    }    
}
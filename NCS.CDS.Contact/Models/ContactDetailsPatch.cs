using System;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Models
{
    public class ContactDetailsPatch : IContactDetails
    {
        [Example(Description = "3")]
        [Display(Description = "Customers preferred contact method   :   " +
                                "1 - Email,   " +
                                "2 - Mobile,   " +
                                "3 - Telephone,   " +
                                "4 - SMS,   " +
                                "5 - Post,   " +
                                "99 - Not known")]
        public PreferredContactMethod? PreferredContactMethod { get; set; }

        [StringLength(20)]
        [Example(Description = "0777 777777")]
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
            if (!LastModifiedDate.HasValue)
                LastModifiedDate = DateTime.UtcNow;
        }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Models
{
    public class ContactDetailsPatch
    {
        [Example(Description = "3")]
        public PreferredContactMethod? PreferredContactMethodID { get; set; }

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

        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? LastModifiedTouchpointID { get; set; }

    }
}

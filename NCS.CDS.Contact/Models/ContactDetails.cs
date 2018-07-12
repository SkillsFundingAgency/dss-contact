using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.ContactDetails.ReferenceData;
using NCS.DSS.ContactDetails.Annotations;

namespace NCS.DSS.ContactDetails.Models
{
    public class ContactDetails
    {
        [Display(Description = "Unique identifier for a contact record")]
        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
        public Guid? ContactID { get; set; }

        [Required]
        [Display(Description = "Unique identifier of a customer")]
        [Example(Description = "2730af9c-fc34-4c2b-a905-c4b584b0f379")]
        public Guid? CustomerId { get; set; }

        [Example(Description = "3")]
        public PreferredContactMethod PreferredContactMethodID { get; set; }

        [RegularExpression(@"^(\+44\s?7\d{3}|\(?07\d{3}\)?)\s?\d{3}\s?\d{3}$")]
        [StringLength(20)]
        [Example(Description = "b8592ff8-af97-49ad-9fb2-e5c3c717fd85")]
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
        public string EmailcontactDetails { get; set; }

        [Example(Description = "2018-06-21T17:45:00")]
        public DateTime LastModifiedDate { get; set; }

        [Example(Description = "12345678")]
        public Guid LastModifiedTouchpointID { get; set; }


        public void Patch(ContactDetailsPatch contactdetailsPatch)
        {
            if (contactdetailsPatch == null)
                return;

            this.PreferredContactMethodID = contactdetailsPatch.PreferredContactMethodID;
            this.AlternativeNumber = contactdetailsPatch.AlternativeNumber;
            this.EmailcontactDetails = contactdetailsPatch.EmailcontactDetails;
            this.HomeNumber = contactdetailsPatch.HomeNumber;
            this.MobileNumber = contactdetailsPatch.MobileNumber;
            this.LastModifiedTouchpointID = contactdetailsPatch.LastModifiedTouchpointID;
            this.LastModifiedDate = contactdetailsPatch.LastModifiedDate;
        }

    }
    
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.ContactDetails.ReferenceData;

namespace NCS.DSS.ContactDetails.Models
{
    public class ContactDetails
    {
        public Guid ContactID { get; set; }

        public PreferredContactMethod PreferredContactMethodID { get; set; }

        [RegularExpression(@"^(\+44\s?7\d{3}|\(?07\d{3}\)?)\s?\d{3}\s?\d{3}$")]
        [StringLength(20)]
        public string MobileNumber { get; set; }

        [StringLength(20)]
        public string HomeNumber { get; set; }

        [StringLength(20)]
        public string AlternativeNumber { get; set; }

        [RegularExpression(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$")]
        [StringLength(255)]
        public string EmailAddress { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public Guid LastModifiedTouchpointID { get; set; }

    }
}

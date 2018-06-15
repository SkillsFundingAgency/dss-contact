using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.Contact.Models
{
    public class Contact
    {
        public Guid ContactID { get; set; }

        public int PreferredContactMethodID { get; set; }

        [StringLength(20)]
        public string MobileNumber { get; set; }

        [StringLength(20)]
        public string HomeNumber { get; set; }

        [StringLength(20)]
        public string AlternativeNumber { get; set; }

        [StringLength(255)]
        public string EmailAddress { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public Guid LastModifiedTouchpointID { get; set; }

    }
}

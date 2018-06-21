using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.ContactDetails.ReferenceData
{
    public enum PreferredContactMethod
    {
        [Description("Email")]
        Email = 1,

        [Description("Mobile")]
        Mobile = 2,

        [Description("Telephone")]
        Telephone = 3,

        [Description("SMS")]
        SMS = 4,

        [Description("Post")]
        Post = 5,

        [Description("Not Known")]
        NotKnown = 99
    }
}

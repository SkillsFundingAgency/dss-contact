using System.ComponentModel;

namespace NCS.DSS.Contact.ReferenceData
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

        [Description("Not Known")]
        NotKnown = 99
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using DFC.Swagger.Standard.Annotations;
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
        [Example(Description = "UK mobile phone number with optional +44 national code, also allows optional brackets and spaces at appropriate positions e.g.   07222 555555 , (07222) 555555 , +44 7222 555 555 or 07222 55555, (07222) 55555, +44 7222 555 55")]
        [RegularExpression(@"^(\+44\s?7\d{3}|\(?07\d{3}\)?)\s?\d{3}\s?(\d{3}|\d{2})$")]
        public string MobileNumber { get; set; }

        [StringLength(20)]
        [Example(Description = "UK phone number. Allows 3, 4 or 5 digit regional prefix, with 8/7, 7/6 or 6/5 digit phone number respectively, plus optional 3 or 4 digit extension number prefixed with a # symbol. " +
                               "Also allows optional brackets surrounding the regional prefix and optional spaces between appropriate groups of numbers    e.g.   " +
                               "01222 555 555   or   (010) 55555555 #2222   or   0122 555 5555#222")]
        [RegularExpression(@"^((\(?0\d{4}\)?\s?\d{3}\s?(\d{3}|\d{2}))|(\(?0\d{3}\)?\s?\d{3}\s?(\d{4}|\d{3}))|(\(?0\d{2}\)?\s?\d{4}\s?(\d{4}|\d{3})))(\s?\#(\d{4}|\d{3}))?$")]
        public string HomeNumber { get; set; }

        [StringLength(20)]
        [Example(Description = "Alternative UK phone number. Allows 3, 4 or 5 digit regional prefix, with 8/7, 7/6 or 6/5 digit phone number respectively, plus optional 3 or 4 digit extension number prefixed with a # symbol. " +
                               "Also allows optional brackets surrounding the regional prefix and optional spaces between appropriate groups of numbers    e.g.   " +
                               "01222 555 555   or   (010) 55555555 #2222   or   0122 555 5555#222")]
        [RegularExpression(@"^((\(?0\d{4}\)?\s?\d{3}\s?(\d{3}|\d{2}))|(\(?0\d{3}\)?\s?\d{3}\s?(\d{4}|\d{3}))|(\(?0\d{2}\)?\s?\d{4}\s?(\d{4}|\d{3})))(\s?\#(\d{4}|\d{3}))?$")]
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

using DFC.Swagger.Standard.Annotations;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.ReferenceData;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

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

        [Required]
        [Display(Description = "Customers preferred contact method   :   " +
                                "1 - Email,   " +
                                "2 - Mobile,   " +
                                "3 - Telephone,   " +
                                "4 - SMS,   " +
                                "5 - Post,   " +
                                "6 - WhatsApp,   " +
                                "99 - Not known")]
        [Example(Description = "3")]
        [JsonConverter(typeof(PermissiveEnumConverter))]
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

        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool? IsDigitalAccount { get; set; }
        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string FirstName { get; private set; }
        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string LastName { get; private set; }

        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool? ChangeEmailAddress { get; private set; }
        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string  CurrentEmail { get; private set; }
        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public string NewEmail { get; private set; }
        [IgnoreDataMember]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public Guid? IdentityStoreId { get; private set; }


        public void SetDefaultValues()
        {
            ContactId = Guid.NewGuid();

            if (!LastModifiedDate.HasValue)
                LastModifiedDate = DateTime.UtcNow;
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

            if(contactdetailsPatch.AlternativeNumber != null)
                AlternativeNumber = contactdetailsPatch.AlternativeNumber;

            if(contactdetailsPatch.EmailAddress != null)
                EmailAddress = contactdetailsPatch.EmailAddress;

            if (contactdetailsPatch.HomeNumber != null)
                HomeNumber = contactdetailsPatch.HomeNumber;

            if (contactdetailsPatch.MobileNumber != null)
                MobileNumber = contactdetailsPatch.MobileNumber;

            if(contactdetailsPatch.LastModifiedDate.HasValue)
                LastModifiedDate = contactdetailsPatch.LastModifiedDate;

            if (!string.IsNullOrEmpty(contactdetailsPatch.LastModifiedTouchpointId))
                LastModifiedTouchpointId = contactdetailsPatch.LastModifiedTouchpointId;

        }

        public void SetDigitalAccountEmailChanged(string newEmail, Guid storeId)
        {
            IsDigitalAccount = true;
            ChangeEmailAddress = true;
            NewEmail = newEmail;
            CurrentEmail = EmailAddress;
            IdentityStoreId = storeId;
        }
    }    
}
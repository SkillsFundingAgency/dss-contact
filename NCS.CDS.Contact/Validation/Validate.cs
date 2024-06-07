using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Validation
{
    public class Validate : IValidate
    {
        public List<ValidationResult> ValidateResource(IContactDetails resource, ContactDetails contactdetails, bool validateModelForPost)
        {
            var context = new ValidationContext(resource, null, null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(resource, context, results, true);
            ValidateContactDetailsRules(resource, results, contactdetails, validateModelForPost);

            return results;
        }

        private void ValidateContactDetailsRules(IContactDetails contactDetailsResource, List<ValidationResult> results, ContactDetails contactDetails, bool validateModelForPost)
        {
            if (contactDetailsResource == null)
                return;
            //Check PreferredContactMethod being patched
            ValidatePreferredContact(contactDetailsResource, results, contactDetails,
                validateModelForPost, contactDetailsResource.PreferredContactMethod);

            //if PreferredContactMethod is empty in the request check against one already in record
            if (!contactDetailsResource.PreferredContactMethod.HasValue && contactDetails != null)
            {
                ValidatePreferredContact(contactDetailsResource, results, contactDetails,
                    validateModelForPost, contactDetails.PreferredContactMethod);
            }

            if (contactDetailsResource.LastModifiedDate.HasValue && contactDetailsResource.LastModifiedDate.Value > DateTime.UtcNow)
                results.Add(new ValidationResult("Last Modified Date must be less the current date/time", new[] { "LastModifiedDate" }));

            if (contactDetailsResource.PreferredContactMethod.HasValue && (!Enum.IsDefined(typeof(PreferredContactMethod), contactDetailsResource.PreferredContactMethod.Value) || contactDetailsResource.PreferredContactMethod.Value.Equals(PreferredContactMethod.Unknown)))
                results.Add(new ValidationResult("Please supply a valid Preferred Contact Method", new[] { "PreferredContactMethod" }));
        }

        private void ValidatePreferredContact(IContactDetails contactDetailsResource, List<ValidationResult> results, ContactDetails contactDetails, bool validateModelForPost, PreferredContactMethod? preferredContactMethod)
        {
            //New validation for empty strings
            if (validateModelForPost)
            {
                switch (preferredContactMethod)
                {
                    case PreferredContactMethod.Email:
                        if (string.IsNullOrWhiteSpace(contactDetailsResource.EmailAddress))
                            results.Add(new ValidationResult("Preferred Contact Method is Email so Email Address must be supplied.", new[] { "EmailAddress" }));
                        break;

                    case PreferredContactMethod.Mobile:
                        if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                            results.Add(new ValidationResult("Preferred Contact Method is Mobile so Mobile Number must be supplied.", new[] { "MobileNumbers", "AlternativeNumber" }));
                        break;

                    case PreferredContactMethod.Telephone:
                        if (string.IsNullOrWhiteSpace(contactDetailsResource.HomeNumber))
                            results.Add(new ValidationResult("Preferred Contact Method is Telephone so Home Number must be supplied.", new[] { "HomeNumber", "AlternativeNumber" }));
                        break;

                    case PreferredContactMethod.SMS:
                        if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                            results.Add(new ValidationResult("Preferred Contact Method is SMS so Mobile Number must be supplied.", new[] { "MobileNumbers" }));
                        break;

                    case PreferredContactMethod.WhatsApp:
                        if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                            results.Add(new ValidationResult("Preferred Contact Method is WhatsApp so Mobile Number must be supplied.", new[] { "MobileNumbers" }));
                        break;
                }
            }
            else
            {
                switch (preferredContactMethod)
                {
                    case PreferredContactMethod.Email:
                        if (contactDetailsResource.EmailAddress == "" ||
                            (string.IsNullOrWhiteSpace(contactDetailsResource.EmailAddress) && string.IsNullOrWhiteSpace(contactDetails.EmailAddress)))
                            results.Add(new ValidationResult("Preferred Contact Method is Email so Email Address must be supplied.", new[] { "EmailAddress" }));
                        break;

                    case PreferredContactMethod.Mobile:
                        if (contactDetailsResource.MobileNumber == "" ||
                            (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber) && string.IsNullOrWhiteSpace(contactDetails.MobileNumber)))
                            results.Add(new ValidationResult("Preferred Contact Method is Mobile so Mobile Number must be supplied.", new[] { "MobileNumbers", "AlternativeNumber" }));
                        break;

                    case PreferredContactMethod.Telephone:
                        if (contactDetailsResource.HomeNumber == "" ||
                            (string.IsNullOrWhiteSpace(contactDetailsResource.HomeNumber) && string.IsNullOrWhiteSpace(contactDetails.HomeNumber)))
                            results.Add(new ValidationResult("Preferred Contact Method is Telephone so Home Number must be supplied.", new[] { "HomeNumber", "AlternativeNumber" }));
                        break;

                    case PreferredContactMethod.SMS:
                        if (contactDetailsResource.MobileNumber == "" ||
                            (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber) && string.IsNullOrWhiteSpace(contactDetails.MobileNumber)))
                            results.Add(new ValidationResult("Preferred Contact Method is SMS so Mobile Number must be supplied.", new[] { "MobileNumbers" }));
                        break;
                    case PreferredContactMethod.WhatsApp:
                        if (contactDetailsResource.MobileNumber == "" ||
                            (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber) && string.IsNullOrWhiteSpace(contactDetails.MobileNumber)))
                            results.Add(new ValidationResult("Preferred Contact Method is WhatsApp so Mobile Number must be supplied.", new[] { "MobileNumbers" }));
                        break;
                }
            }
        }
    }
}

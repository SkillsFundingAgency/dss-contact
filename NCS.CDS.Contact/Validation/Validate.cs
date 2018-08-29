using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Validation
{
    public class Validate : IValidate
    {
        public List<ValidationResult> ValidateResource(IContactDetails resource, bool validateModelForPost)
        {
            var context = new ValidationContext(resource, null, null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(resource, context, results, true);
            ValidateContactDetailsRules(resource, results, validateModelForPost);

            return results;
        }

        private void ValidateContactDetailsRules(IContactDetails contactDetailsResource, List<ValidationResult> results, bool validateModelForPost)
        {
            if (contactDetailsResource == null)
                return;

            if (validateModelForPost)
            {

                if(contactDetailsResource.PreferredContactMethod == PreferredContactMethod.Mobile &&
                   (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber) || 
                   string.IsNullOrWhiteSpace(contactDetailsResource.AlternativeNumber)))
                    results.Add(new ValidationResult("Mobile Number or Alternative Number must be supplied.", new[] { "MobileNumbers", "AlternativeNumber" }));

                if (contactDetailsResource.PreferredContactMethod == PreferredContactMethod.Telephone && 
                    (string.IsNullOrWhiteSpace(contactDetailsResource.HomeNumber) ||
                    string.IsNullOrWhiteSpace(contactDetailsResource.AlternativeNumber)))
                    results.Add(new ValidationResult("Home Number or Alternative Number must be supplied.", new[] { "HomeNumber", "AlternativeNumber" }));

                if (contactDetailsResource.PreferredContactMethod == PreferredContactMethod.SMS &&
                    string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                    results.Add(new ValidationResult("Mobile Number must be supplied.", new[] { "MobileNumbers" }));

                if (contactDetailsResource.PreferredContactMethod == PreferredContactMethod.Email &&
                    string.IsNullOrWhiteSpace(contactDetailsResource.EmailAddress))
                    results.Add(new ValidationResult("Email Address must be supplied.", new[] { "EmailAddress" }));

            }

            if (contactDetailsResource.LastModifiedDate.HasValue && contactDetailsResource.LastModifiedDate.Value > DateTime.UtcNow)
                results.Add(new ValidationResult("Last Modified Date must be less the current date/time", new[] { "LastModifiedDate" }));

            if (contactDetailsResource.PreferredContactMethod.HasValue && !Enum.IsDefined(typeof(PreferredContactMethod), contactDetailsResource.PreferredContactMethod.Value))
                results.Add(new ValidationResult("Please supply a valid Preferred Contact Method", new[] { "PreferredContactMethod" }));

        }

    }
}

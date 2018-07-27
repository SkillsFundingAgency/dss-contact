using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Validation
{
    public class Validate : IValidate
    {
        public List<ValidationResult> ValidateResource(IContactDetails resource)
        {
            var context = new ValidationContext(resource, null, null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(resource, context, results, true);
            ValidateContactDetailsRules(resource, results);

            return results;
        }

        private void ValidateContactDetailsRules(IContactDetails contactDetailsResource, List<ValidationResult> results)
        {
            if (contactDetailsResource == null)
                return;

            if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber) &&
                string.IsNullOrWhiteSpace(contactDetailsResource.HomeNumber) &&
                string.IsNullOrWhiteSpace(contactDetailsResource.AlternativeNumber) &&
                string.IsNullOrWhiteSpace(contactDetailsResource.EmailAddress))
                results.Add(new ValidationResult("At least one of the following fields 'Mobile Number', 'Home Number', 'Alternative Number' or 'Email Address' must be supplied.", 
                    new[] { "MobileNumber", "HomeNumber", "AlternativeNumber", "EmailAddress" }));

             if (contactDetailsResource.LastModifiedDate.HasValue && contactDetailsResource.LastModifiedDate.Value > DateTime.UtcNow)
                results.Add(new ValidationResult("Last Modified Date must be less the current date/time", new[] { "LastModifiedDate" }));

            if (contactDetailsResource.PreferredContactMethod.HasValue && !Enum.IsDefined(typeof(PreferredContactMethod), contactDetailsResource.PreferredContactMethod.Value))
                results.Add(new ValidationResult("Please supply a valid Preferred Contact Method", new[] { "PreferredContactMethod" }));

        }

    }
}

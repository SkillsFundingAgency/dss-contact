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


            switch (contactDetailsResource.PreferredContactMethod)
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
            }
            

            if (contactDetailsResource.LastModifiedDate.HasValue && contactDetailsResource.LastModifiedDate.Value > DateTime.UtcNow)
                results.Add(new ValidationResult("Last Modified Date must be less the current date/time", new[] { "LastModifiedDate" }));

            if (contactDetailsResource.PreferredContactMethod.HasValue && !Enum.IsDefined(typeof(PreferredContactMethod), contactDetailsResource.PreferredContactMethod.Value))
                results.Add(new ValidationResult("Please supply a valid Preferred Contact Method", new[] { "PreferredContactMethod" }));
        }
    }
}

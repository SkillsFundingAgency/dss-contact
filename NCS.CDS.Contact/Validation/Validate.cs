using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;

namespace NCS.DSS.Contact.Validation
{
    public class Validate : IValidate
    {
        private readonly IDocumentDBProvider _documentDbProvider;
        public Validate(IDocumentDBProvider documentDBProvider)
        {
            _documentDbProvider = documentDBProvider;
        }

        public async Task<List<ValidationResult>> ValidateResource(IContactDetails resource, bool validateModelForPost)
        {
            var context = new ValidationContext(resource, null, null);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(resource, context, results, true);
            await ValidateContactDetailsRules(resource, results, validateModelForPost);

            return results;
        }

        private async Task ValidateContactDetailsRules(IContactDetails contactDetailsResource, List<ValidationResult> results, bool validateModelForPost)
        {
            if (contactDetailsResource != null)
            {
                if (validateModelForPost)
                {
                    switch (contactDetailsResource.PreferredContactMethod)
                    {
                        case PreferredContactMethod.Email:
                            if (string.IsNullOrWhiteSpace(contactDetailsResource.EmailAddress))
                                results.Add(new ValidationResult("Email Address must be supplied.", new[] { "EmailAddress" }));
                            break;

                        case PreferredContactMethod.Mobile:
                            if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                                results.Add(new ValidationResult("Mobile Number must be supplied.", new[] { "MobileNumbers", "AlternativeNumber" }));
                            break;

                        case PreferredContactMethod.Telephone:
                            if (string.IsNullOrWhiteSpace(contactDetailsResource.HomeNumber))
                                results.Add(new ValidationResult("Home Number must be supplied.", new[] { "HomeNumber", "AlternativeNumber" }));
                            break;

                        case PreferredContactMethod.SMS:
                            if (string.IsNullOrWhiteSpace(contactDetailsResource.MobileNumber))
                                results.Add(new ValidationResult("Mobile Number must be supplied.", new[] { "MobileNumbers" }));
                            break;
                    }
                }

                if (contactDetailsResource.LastModifiedDate.HasValue && contactDetailsResource.LastModifiedDate.Value > DateTime.UtcNow)
                    results.Add(new ValidationResult("Last Modified Date must be less the current date/time", new[] { "LastModifiedDate" }));

                if (contactDetailsResource.PreferredContactMethod.HasValue && !Enum.IsDefined(typeof(PreferredContactMethod), contactDetailsResource.PreferredContactMethod.Value))
                    results.Add(new ValidationResult("Please supply a valid Preferred Contact Method", new[] { "PreferredContactMethod" }));

                // TODO : Only do this if account has digital identity
                var doesContactWithEmailExists = await _documentDbProvider.DoesContactDetailsWithEmailExists(contactDetailsResource.EmailAddress);

                if (doesContactWithEmailExists)
                    results.Add(new ValidationResult("Contact with Email Address contactDetailsResource.EmailAddress already exists.", new[] { "EmailAddress" }));
            }
        }
    }
}

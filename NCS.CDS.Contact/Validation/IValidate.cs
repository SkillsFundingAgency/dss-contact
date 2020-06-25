using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Validation
{
    public interface IValidate
    {
        List<ValidationResult> ValidateResource(IContactDetails resource, ContactDetails contactdetails, bool validateModelForPost);
    }
}
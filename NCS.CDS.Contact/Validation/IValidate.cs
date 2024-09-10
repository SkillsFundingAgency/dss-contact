using NCS.DSS.Contact.Models;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.Contact.Validation
{
    public interface IValidate
    {
        List<ValidationResult> ValidateResource(IContactDetails resource, ContactDetails contactdetails, bool validateModelForPost);
    }
}
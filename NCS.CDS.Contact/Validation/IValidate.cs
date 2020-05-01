using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Validation
{
    public interface IValidate
    {
        Task<List<ValidationResult>> ValidateResource(IContactDetails resource, bool validateModelForPost);
    }
}
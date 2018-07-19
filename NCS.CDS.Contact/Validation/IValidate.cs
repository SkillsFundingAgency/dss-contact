using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.Contact.Validation
{
    public interface IValidate
    {
        List<ValidationResult> ValidateResource<T>(T resource);
    }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.ContactDetails.Validation
{
    public interface IValidate
    {
        List<ValidationResult> ValidateResource<T>(T resource);
    }
}
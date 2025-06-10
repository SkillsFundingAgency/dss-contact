using DFC.Swagger.Standard.Annotations;
using System.ComponentModel.DataAnnotations;

namespace NCS.DSS.Contact.Models
{
    public class Customer : ICustomer
    {
        [Display(Description = "Date the customer terminated their account")]
        [Example(Description = "2018-06-21T14:45:00")]
        public DateTime? DateOfTermination { get; set; }
    }    
}

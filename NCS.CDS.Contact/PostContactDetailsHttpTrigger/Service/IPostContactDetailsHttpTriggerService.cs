using System.Threading.Tasks;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public interface IPostContactDetailsHttpTriggerService
    {
        Task<Contact.Models.ContactDetails> CreateAsync(Contact.Models.ContactDetails contactdetails);
    }
}

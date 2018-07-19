using System.Threading.Tasks;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public interface IPostContactDetailsHttpTriggerService
    {
        Task<Contact.Models.ContactDetails> CreateContactDetails(Contact.Models.ContactDetails contactdetails);
    }
}

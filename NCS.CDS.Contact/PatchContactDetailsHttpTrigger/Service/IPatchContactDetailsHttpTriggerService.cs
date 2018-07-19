using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service
{
    public interface IPatchContactDetailsHttpTriggerService
    {
        Task<Contact.Models.ContactDetails> UpdateAsync(Contact.Models.ContactDetails contactdetails, ContactDetailsPatch contactdetailsPatch);
        Task<Contact.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactdetailsId);

    }
}

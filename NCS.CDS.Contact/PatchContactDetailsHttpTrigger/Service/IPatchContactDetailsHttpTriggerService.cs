using System;
using System.Threading.Tasks;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service
{
    public interface IPatchContactDetailsHttpTriggerService
    {
        Task<ContactDetails> UpdateAsync(ContactDetails contactdetails, ContactDetailsPatch contactdetailsPatch);
        Task<ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactdetailsId);
        Task SendToServiceBusQueueAsync(Models.ContactDetails contactdetails, Guid customerId, string reqUrl);
    }
}

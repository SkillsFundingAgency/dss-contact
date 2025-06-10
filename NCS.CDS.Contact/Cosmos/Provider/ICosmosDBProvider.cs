using Microsoft.Azure.Cosmos;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
{
    public interface ICosmosDBProvider
    {
        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<bool> DoesCustomerHaveATerminationDate(Guid customerId);
        Task<bool> DoesContactDetailsExistForCustomer(Guid customerId);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId);
        Task<ItemResponse<ContactDetails>> CreateContactDetailsAsync(ContactDetails contactDetails);
        Task<ItemResponse<ContactDetails>> UpdateContactDetailsAsync(ContactDetails contactDetails);
        Task<bool> DoesContactDetailsWithEmailExists(string email);
        Task<bool> DoesContactDetailsWithEmailExistsForAnotherCustomer(string email, Guid customerId);
        Task<IList<ContactDetails>> GetContactsByEmail(string email);
        Task<DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
    }
}
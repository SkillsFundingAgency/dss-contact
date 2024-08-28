using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<bool> DoesCustomerHaveATerminationDate(Guid customerId);
        bool DoesContactDetailsExistForCustomer(Guid customerId);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId);
        Task<ResourceResponse<Document>> CreateContactDetailsAsync(ContactDetails contactDetails);
        Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails);
        Task<bool> DoesContactDetailsWithEmailExists(string email);
        Task<bool> DoesContactDetailsWithEmailExistsForAnotherCustomer(string email, Guid CustomerId);
        Task<DigitalIdentity> GetIdentityForCustomerAsync(Guid customerId);
        Task<IList<ContactDetails>> GetContactsByEmail(string email);

    }
}
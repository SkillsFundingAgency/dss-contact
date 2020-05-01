using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        Task<bool> DoesCustomerResourceExist(Guid customerId);
        Task<bool> DoesCustomerHaveATerminationDate(Guid customerId);
        Task<bool> DoesContactDetailsExistForCustomer(Guid customerId);
        Task<bool> DoesContactDetailsWithEmailExists(string emailAddressToCheck);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId);
        Task<ContactDetails> GetContactDetailForCustomerAsync(Guid customerId, Guid contactDetailsId);
        Task<ResourceResponse<Document>> CreateContactDetailsAsync(ContactDetails contactDetails);
        Task<ResourceResponse<Document>> UpdateContactDetailsAsync(ContactDetails contactDetails);

    }
}
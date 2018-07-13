using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.ContactDetails.Cosmos.Provider
{
    public interface IDocumentDBProvider
    {
        bool DoesCustomerResourceExist(Guid customerId);
        Task<ResourceResponse<Document>> CreateContactDetailsAsync(Models.ContactDetails contactDetails);
        Task<ResourceResponse<Document>> UpdateContactDetailsAsync(Models.ContactDetails contactDetails);

    }
}
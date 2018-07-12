using System;

namespace NCS.DSS.ContactDetails.Cosmos.Helper
{
    public interface IDocumentDBHelper
    {
        Uri CreateDocumentCollectionUri();
        Uri CreateDocumentUri(Guid contactDetailsId);
        Uri CreateCustomerDocumentCollectionUri();
    }
}
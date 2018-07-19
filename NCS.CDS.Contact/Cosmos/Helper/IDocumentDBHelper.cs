using System;

namespace NCS.DSS.Contact.Cosmos.Helper
{
    public interface IDocumentDBHelper
    {
        Uri CreateDocumentCollectionUri();
        Uri CreateDocumentUri(Guid contactDetailsId);
        Uri CreateCustomerDocumentCollectionUri();
    }
}
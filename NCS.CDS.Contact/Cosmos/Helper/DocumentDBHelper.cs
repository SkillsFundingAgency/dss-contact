
using System;
using System.Configuration;
using Microsoft.Azure.Documents.Client;

namespace NCS.DSS.Contact.Cosmos.Helper
{
    public static class DocumentDBHelper
    {
        private static Uri _documentCollectionUri;
        private static Uri _documentUri;
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["DatabaseId"];
        private static readonly string CollectionId = ConfigurationManager.AppSettings["CollectionId"];

        private static Uri _customerDocumentCollectionUri;
        private static readonly string CustomerDatabaseId = ConfigurationManager.AppSettings["CustomerDatabaseId"];
        private static readonly string CustomerCollectionId = ConfigurationManager.AppSettings["CustomerCollectionId"];

        public static Uri CreateDocumentCollectionUri()
        {
            if (_documentCollectionUri != null)
                return _documentCollectionUri;

            _documentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                DatabaseId,
                CollectionId);

            return _documentCollectionUri;
        }


        public static Uri CreateDocumentUri(Guid contactDetailsId)
        {
            if (_documentUri != null)
                return _documentUri;

            _documentUri = UriFactory.CreateDocumentUri(DatabaseId, CollectionId, contactDetailsId.ToString());

            return _documentUri;

        }

        #region CustomerDB

        public static Uri CreateCustomerDocumentCollectionUri()
        {
            if (_customerDocumentCollectionUri != null)
                return _customerDocumentCollectionUri;

            _customerDocumentCollectionUri = UriFactory.CreateDocumentCollectionUri(
                CustomerDatabaseId, CustomerCollectionId);

            return _customerDocumentCollectionUri;
        }

        #endregion   

    }
}

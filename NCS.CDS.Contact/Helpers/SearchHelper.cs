using System;
using System.Configuration;
using Azure;
using Azure.Search;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

namespace NCS.DSS.Contact.Helpers
{
    public static class SearchHelper
    {
        private static readonly string SearchServiceName = Environment.GetEnvironmentVariable("SearchServiceName");
        private static readonly string SearchServiceKey = Environment.GetEnvironmentVariable("SearchServiceAdminApiKey");
        private static readonly string SearchServiceIndexName = Environment.GetEnvironmentVariable("CustomerSearchIndexName");

        private static SearchIndexerClient _serviceClient;
        private static SearchClient _indexClient;

        public static SearchIndexerClient GetSearchServiceClient()
        {
            if (_serviceClient != null)
                return _serviceClient;

            _serviceClient = new SearchIndexerClient(new Uri($"https://{SearchServiceName}.search.windows.net"), new AzureKeyCredential(SearchServiceKey));

            return _serviceClient;
        }
        public static SearchClient GetIndexClient()
        {
            if (_indexClient != null)
                return _indexClient;

            /*            _indexClient = _serviceClient?.Indexes?.GetClient(SearchServiceIndexName);*/
            _indexClient = new SearchClient(new Uri($"https://{SearchServiceName}.search.windows.net"), SearchServiceIndexName, new AzureKeyCredential(SearchServiceKey));

            return _indexClient;
        }
    }
}
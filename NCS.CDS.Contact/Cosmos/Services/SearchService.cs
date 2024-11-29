using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Services
{
    public class SearchService : ISearchService
    {
        private readonly string _searchServiceName;
        private readonly string _searchServiceKey;
        private readonly string _searchIndexName;
        private readonly Lazy<SearchIndexerClient> _indexerClient;
        private readonly Lazy<SearchClient> _searchClient;

        public SearchService(ContactConfigurationSettings contactConfigurationSettings)
        {
            _searchServiceName = contactConfigurationSettings.SearchServiceName ??
                                 throw new ArgumentNullException(nameof(contactConfigurationSettings.SearchServiceName));
            _searchServiceKey = contactConfigurationSettings.SearchServiceKey ??
                                throw new ArgumentNullException(nameof(contactConfigurationSettings.SearchServiceKey));
            _searchIndexName = contactConfigurationSettings.SearchServiceIndexName ??
                               throw new ArgumentNullException(nameof(contactConfigurationSettings.SearchServiceIndexName));

            _indexerClient = new Lazy<SearchIndexerClient>(() => 
                new SearchIndexerClient(new Uri($"https://{_searchServiceName}.search.windows.net"), new AzureKeyCredential(_searchServiceKey)));
            _searchClient = new Lazy<SearchClient>(() => 
                new SearchClient(new Uri($"https://{_searchServiceName}.search.windows.net"), _searchIndexName, new AzureKeyCredential(_searchServiceKey)));
        }

        public SearchIndexerClient GetSearchIndexerClient() => _indexerClient.Value;

        public SearchClient GetSearchClient() => _searchClient.Value;
    }
}
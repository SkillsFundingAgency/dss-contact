using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;
using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.Cosmos.Services
{
    public class SearchService : ISearchService
    {
        private readonly Lazy<SearchIndexerClient> _indexerClient;
        private readonly Lazy<SearchClient> _searchClient;

        public SearchService(IOptions<ContactConfigurationSettings> configOptions)
        {
            var config = configOptions.Value;
            var searchServiceName = config.SearchServiceName ??
                                     throw new ArgumentNullException(nameof(config.SearchServiceName));
            var searchServiceKey = config.SearchServiceKey ??
                                    throw new ArgumentNullException(nameof(config.SearchServiceKey));
            var searchIndexName = config.SearchServiceIndexName ??
                                   throw new ArgumentNullException(nameof(config.SearchServiceIndexName));

            _indexerClient = new Lazy<SearchIndexerClient>(() => 
                new SearchIndexerClient(new Uri($"https://{searchServiceName}.search.windows.net"), new AzureKeyCredential(searchServiceKey)));
            _searchClient = new Lazy<SearchClient>(() => 
                new SearchClient(new Uri($"https://{searchServiceName}.search.windows.net"), searchIndexName, new AzureKeyCredential(searchServiceKey)));
        }

        public SearchIndexerClient GetSearchIndexerClient() => _indexerClient.Value;

        public SearchClient GetSearchClient() => _searchClient.Value;
    }
}
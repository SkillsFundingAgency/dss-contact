using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

namespace NCS.DSS.Contact.Cosmos.Services
{
    public interface ISearchService
    {
        SearchIndexerClient GetSearchIndexerClient();
        SearchClient GetSearchClient();
    }
}
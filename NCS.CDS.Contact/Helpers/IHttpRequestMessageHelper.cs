using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.ContactDetails.Helpers
{
    public interface IHttpRequestMessageHelper
    {
        Task<T> GetContactDetailsFromRequest<T>(HttpRequestMessage req);
    }
}
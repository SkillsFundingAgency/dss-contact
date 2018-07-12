using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.ContactDetails.Helpers
{
    public class HttpRequestMessageHelper : IHttpRequestMessageHelper
    {
        public async Task<T> GetcontactDetailsFromRequest<T>(HttpRequestMessage req)
        {
            return await req.Content.ReadAsAsync<T>();
        }
    }
}

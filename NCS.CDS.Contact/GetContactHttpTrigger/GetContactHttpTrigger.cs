using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http.Description;
using NCS.DSS.ContactDetails.Annotations;

namespace NCS.DSS.ContactDetails.GetContactHttpTrigger
{
    public static class GetContactHttpTrigger
    {
        [FunctionName("GET")]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Created, Description = "Contact Details Retrieved", ShowSchema = true)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Unable to Retrieve Contact Details", ShowSchema = false)]
        [ContactDetailsResponse(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Forbidden", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/ContactDetails/")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function GetContact processed a request.");

            var service = new GetContactHttpTriggerService();
            var values = await service.GetContactDetails();
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(values),
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

    }
}

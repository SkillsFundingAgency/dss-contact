using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Web.Http.Description;
using NCS.DSS.ContactDetails.Annotations;

namespace NCS.DSS.ContactDetails.PutContactHttpTrigger
{
    public static class PutContactHttpTrigger
    {
        [FunctionName("PUT")]
        [Response(HttpStatusCode = (int)HttpStatusCode.Created, Description = "Contact Details Replaced", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Unable to Replace Customer", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "Unauthorised", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{customerId}/ContactDetails/{contactId}")]HttpRequestMessage req, TraceWriter log, string customerId, string contactid)
        {
            log.Info("C# HTTP trigger function PutContact processed a request.");

            //if (!Guid.TryParse(contactid, out var contactGuid))
            //{
            //    return new HttpResponseMessage(HttpStatusCode.BadRequest)
            //    {
            //        Content = new StringContent(JsonConvert.SerializeObject(contactid),
            //            System.Text.Encoding.UTF8, "application/json")
            //    };
            //}

            //var values = "Sucessfully Replaced ContactDetails record with id : " + contactid;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(null),
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

    }
}

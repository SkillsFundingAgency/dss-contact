using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Ioc;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Function
{
    public static class GetContactHttpTrigger
    {
        [FunctionName("GET")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [ResponseType(typeof(Models.ContactDetails))]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/")]HttpRequestMessage req, ILogger log, string customerId,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IHttpRequestMessageHelper httpRequestMessageHelper,
            [Inject]IGetContactHttpTriggerService getContactsService)
        {
            var touchpointId = httpRequestMessageHelper.GetTouchpointId(req);
            if (touchpointId == null)
            {
                log.LogInformation("Unable to locate 'APIM-TouchpointId' in request header.");
                return HttpResponseMessageHelper.BadRequest();
            }

            log.LogInformation("C# HTTP trigger function GetContactHttpTrigger processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return HttpResponseMessageHelper.BadRequest(customerGuid);

            var doesCustomerExist = resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return HttpResponseMessageHelper.NoContent(customerGuid);

            var contact = await getContactsService.GetContactDetailsForCustomerAsync(customerGuid);

            return contact == null ?
                HttpResponseMessageHelper.NoContent(customerGuid) :
                HttpResponseMessageHelper.Ok(JsonHelper.SerializeObject(contact));
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Description;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Ioc;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Annotations;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function
{
    public static class PatchContactHttpTrigger
    {
        [FunctionName("PATCH")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ResponseType(typeof(ContactDetails))]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customers/{customerId}/ContactDetails/{contactid}")]HttpRequestMessage req, ILogger log, 
            string customerId, string contactid,
            [Inject]IResourceHelper resourceHelper,
            [Inject]IHttpRequestMessageHelper httpRequestMessageHelper,
            [Inject]IValidate validate,
            [Inject]IPatchContactDetailsHttpTriggerService contactdetailsPatchService)
        {
            var touchpointId = httpRequestMessageHelper.GetTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return HttpResponseMessageHelper.BadRequest();
            }

            var ApimURL = httpRequestMessageHelper.GetApimURL(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                log.LogInformation("Unable to locate 'apimurl' in request header");
                return HttpResponseMessageHelper.BadRequest();
            }

            log.LogInformation("C# HTTP trigger function Patch Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return HttpResponseMessageHelper.BadRequest(customerGuid);

            if (!Guid.TryParse(contactid, out var contactGuid))
                return HttpResponseMessageHelper.BadRequest(contactGuid);

            ContactDetailsPatch contactdetailsPatchRequest;

            try
            {
                contactdetailsPatchRequest = await httpRequestMessageHelper.GetContactDetailsFromRequest<ContactDetailsPatch>(req);
            }
            catch (JsonException ex)
            {
                return HttpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (contactdetailsPatchRequest == null)
                return HttpResponseMessageHelper.UnprocessableEntity(req);

            contactdetailsPatchRequest.LastModifiedTouchpointId = touchpointId;

            var errors = await validate.ValidateResource(contactdetailsPatchRequest, false);

            if (errors != null && errors.Any())
                return HttpResponseMessageHelper.UnprocessableEntity(errors);

            var doesCustomerExist = await resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return HttpResponseMessageHelper.NoContent(customerGuid);

            var isCustomerReadOnly = await resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
                return HttpResponseMessageHelper.Forbidden(customerGuid);

            var contactdetails = await contactdetailsPatchService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

            if (contactdetails == null)
                return HttpResponseMessageHelper.NoContent(contactGuid);

            var updatedContactDetails = await contactdetailsPatchService.UpdateAsync(contactdetails, contactdetailsPatchRequest);

            if (updatedContactDetails != null)
            {
                try
                {
                    await contactdetailsPatchService.SendToServiceBusQueueAsync(updatedContactDetails, customerGuid, ApimURL);
                }
                catch(Exception ex)
                {
                    log.LogError($"Error while sending PATCH request to ServiceBut. {ex.Message}", ex);
                    throw;
                }
            }

            return updatedContactDetails == null ?
                HttpResponseMessageHelper.BadRequest(contactGuid) :
                HttpResponseMessageHelper.Ok(JsonHelper.SerializeObject(updatedContactDetails));
        }

    }
}

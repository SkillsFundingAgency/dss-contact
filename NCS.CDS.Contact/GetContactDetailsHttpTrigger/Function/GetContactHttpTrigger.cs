using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Helpers;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Function
{
    public class GetContactHttpTrigger
    {
        private readonly IResourceHelper _resourceHelper;
        private readonly IHttpResponseMessageHelper _httpResponseMessageHelper;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IGetContactHttpTriggerService _getContactDetailsByIdService;

        public GetContactHttpTrigger(IResourceHelper resourceHelper,
            IHttpRequestHelper httpRequestMessageHelper,
            IGetContactHttpTriggerService getContactsService,
            IHttpResponseMessageHelper httpResponseMessageHelper)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _getContactDetailsByIdService = getContactsService;
            _httpResponseMessageHelper = httpResponseMessageHelper;
        }

        [FunctionName("GET")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.ContactDetails), 200)]
        public async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/")]HttpRequest req, ILogger log, string customerId)
        {
            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return _httpResponseMessageHelper.BadRequest();
            }

            log.LogInformation("C# HTTP trigger function GetContactHttpTrigger processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return _httpResponseMessageHelper.BadRequest(customerGuid);

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return _httpResponseMessageHelper.NoContent(customerGuid);

            var contact = await _getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid);

            return contact == null ?
                _httpResponseMessageHelper.NoContent(customerGuid) :
                _httpResponseMessageHelper.Ok(JsonHelper.SerializeObject(contact));
        }
    }
}

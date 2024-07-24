using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.Helpers;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Text;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function
{
    public class GetContactByIdHttpTrigger
    {
        private readonly IResourceHelper _resourceHelper;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IGetContactDetailsByIdHttpTriggerService _getContactDetailsByIdService;
        private readonly IHttpResponseMessageHelper _httpResponseMessageHelper;

        public GetContactByIdHttpTrigger(IResourceHelper resourceHelper,
            IHttpRequestHelper httpRequestMessageHelper,
            IGetContactDetailsByIdHttpTriggerService getContactDetailsByIdService,
            IHttpResponseMessageHelper httpResponseMessageHelper)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _getContactDetailsByIdService = getContactDetailsByIdService;
            _httpResponseMessageHelper = httpResponseMessageHelper;
        }

        [Function("GETByID")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.ContactDetails), 200)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/{contactid}")] HttpRequest req, ILogger logger, string customerId, string contactid)
        {
            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                logger.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            logger.LogInformation("C# HTTP trigger function GetContactByIdHttpTrigger processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                logger.LogInformation($"No customer with ID [{customerGuid}]");
                return new BadRequestObjectResult(new StringContent(JsonConvert.SerializeObject(customerGuid), Encoding.UTF8, ContentApplicationType.ApplicationJSON));
            }

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                logger.LogInformation($"No contact with ID [{contactGuid}]");
                return new BadRequestObjectResult(new StringContent(JsonConvert.SerializeObject(contactGuid), Encoding.UTF8, ContentApplicationType.ApplicationJSON));
            }

                var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            { 
                logger.LogInformation($"Customer does not exist.");
                    return new NoContentResult();
            }

            var contact = await _getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid, logger);

            return contact == null ?
                new NoContentResult() :
                new OkObjectResult(new StringContent(JsonHelper.SerializeObject(contact),
                    Encoding.UTF8, ContentApplicationType.ApplicationJSON));
        }
    }
}

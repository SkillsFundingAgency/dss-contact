using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using System.Net;
using System.Text.Json;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function
{
    public class GetContactByIdHttpTrigger
    {
        private readonly IResourceHelper _resourceHelper;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IGetContactDetailsByIdHttpTriggerService _getContactDetailsByIdService;
        private readonly ILogger logger;

        public GetContactByIdHttpTrigger(IResourceHelper resourceHelper,
            IHttpRequestHelper httpRequestMessageHelper,
            IGetContactDetailsByIdHttpTriggerService getContactDetailsByIdService,
            ILogger<GetContactByIdHttpTrigger> logger)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _getContactDetailsByIdService = getContactDetailsByIdService;
            this.logger = logger;
        }

        [Function("GETByID")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "Resource Does Not Exist", ShowSchema = false)]
        [ProducesResponseType(typeof(Models.ContactDetails), 200)]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/{contactid}")] HttpRequest req, string customerId, string contactid)
        {
            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                logger.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult("Unable to locate 'TouchpointId' in request header.");
            }

            logger.LogInformation("C# HTTP trigger function GetContactByIdHttpTrigger processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                logger.LogInformation($"Unable to parse 'customerId' to a GUID [{customerId}]");
                return new BadRequestObjectResult($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}.");
            }

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                logger.LogInformation($"Unable to parse 'contactid' to a GUID [{contactid}]");
                return new BadRequestObjectResult($"Unable to parse 'contactid' to a GUID. Contact ID: {contactid}.");
            }

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                logger.LogInformation($"Customer does not exist.");
                return new NotFoundObjectResult($"Customer ({customerGuid}) does not exist.");
            }

            var contact = await _getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid, logger);

            return contact == null
                ? new NotFoundObjectResult($"Contact details ({contactGuid}) for customer ({customerGuid}) do not exist.")
                : new JsonResult(contact, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
        }
    }
}

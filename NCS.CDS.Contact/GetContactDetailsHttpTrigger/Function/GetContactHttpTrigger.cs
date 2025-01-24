using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Models;
using System.Net;
using System.Text.Json;

namespace NCS.DSS.Contact.GetContactDetailsHttpTrigger.Function
{
    public class GetContactHttpTrigger
    {
        private readonly IGetContactHttpTriggerService _getContactDetailsByIdService;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IResourceHelper _resourceHelper;
        private readonly ILogger<GetContactHttpTrigger> _logger;

        public GetContactHttpTrigger(IGetContactHttpTriggerService getContactsService,
            IHttpRequestHelper httpRequestMessageHelper,
            IResourceHelper resourceHelper,
            ILogger<GetContactHttpTrigger> logger)
        {
            _getContactDetailsByIdService = getContactsService;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _resourceHelper = resourceHelper;
            _logger = logger;
        }

        [Function("GET_BY_CUSTOMERID")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "Resource Does Not Exist", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/")]
            HttpRequest req, string customerId)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(GetContactHttpTrigger));

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogError("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult("Unable to locate 'TouchpointId' in request header.");
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogError("Unable to parse 'customerId' to a GUID. Customer ID: {CustomerId}", customerId);
                return new BadRequestObjectResult($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}.");
            }

            _logger.LogInformation("Header validation has succeeded. Touchpoint ID: {TouchpointId}", touchpointId);
            
            _logger.LogInformation("Attempting to see if customer exists. Customer GUID: {CustomerGuid}", customerGuid);
            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                _logger.LogError("Customer does not exist. Customer GUID: {CustomerGuid}", customerGuid);
                return new NotFoundObjectResult($"Customer ({customerGuid}) does not exist.");
            }
            _logger.LogInformation("Customer exists. Customer GUID: {CustomerGuid}", customerGuid);

            _logger.LogInformation("Attempting to retrieve ContactDetails for Customer. Customer GUID: {CustomerGuid}", customerGuid);
            var contact = await _getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid);

            return contact == null
                ? new NotFoundObjectResult("No contact details found for customer ({customerGuid}).")
                : new JsonResult(contact, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };

        }
    }
}

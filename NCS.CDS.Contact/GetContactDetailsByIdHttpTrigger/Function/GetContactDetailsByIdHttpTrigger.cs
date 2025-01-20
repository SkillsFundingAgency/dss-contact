using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.Models;
using System.Net;
using System.Text.Json;

namespace NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function
{
    public class GetContactByIdHttpTrigger
    {
        private readonly IGetContactDetailsByIdHttpTriggerService _getContactDetailsByIdService;
        private readonly IHttpRequestHelper _httpRequestMessageHelper;
        private readonly IResourceHelper _resourceHelper;
        private readonly ILogger<GetContactByIdHttpTrigger> _logger;

        public GetContactByIdHttpTrigger(IGetContactDetailsByIdHttpTriggerService getContactDetailsByIdService,
            IHttpRequestHelper httpRequestMessageHelper,
            IResourceHelper resourceHelper,
            ILogger<GetContactByIdHttpTrigger> logger)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _getContactDetailsByIdService = getContactDetailsByIdService;
            _logger = logger;
        }

        [Function("GET_BY_CONTACTDETAILSID")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Retrieved", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Get request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NotFound, Description = "Resource Does Not Exist", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{customerId}/ContactDetails/{contactid}")]
            HttpRequest req, string customerId, string contactid)
        {
            _logger.LogInformation("Function {FunctionName} has been invoked", nameof(GetContactByIdHttpTrigger));

            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                _logger.LogWarning("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult("Unable to locate 'TouchpointId' in request header.");
            }

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                _logger.LogWarning("Unable to parse 'customerId' to a GUID. Customer ID: {CustomerId}", customerId);
                return new BadRequestObjectResult($"Unable to parse 'customerId' to a GUID. Customer ID: {customerId}.");
            }

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                _logger.LogWarning("Unable to parse 'contactGuid' to a GUID. Customer ID: {ContactGuid}", contactGuid);
                return new BadRequestObjectResult($"Unable to parse 'contactid' to a GUID. Contact ID: {contactid}.");
            }

            _logger.LogInformation("Attempting to see if customer exists. Customer GUID: {CustomerGuid}", customerGuid);
            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            _logger.LogInformation("Header validation has succeeded. Touchpoint ID: {TouchpointId}", touchpointId);

            if (!doesCustomerExist)
            {
                _logger.LogError("Customer does not exist. Customer GUID: {CustomerGuid}", customerGuid);
                return new NotFoundObjectResult($"Customer ({customerGuid}) does not exist.");
            }

            _logger.LogInformation("Customer exists. Customer GUID: {CustomerGuid}", customerGuid);
            _logger.LogInformation("Attempting to retrieve ContactDetails for Customer. Customer GUID: {CustomerGuid}", customerGuid);
            var contact = await _getContactDetailsByIdService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid, _logger);

            if (contact != null)
            {
                _logger.LogInformation("ContactDetails successfully retrieved. Contact Detail ID: {ContactId}", contact.ContactId.GetValueOrDefault());
                _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(GetContactByIdHttpTrigger));
                return new JsonResult(contact, new JsonSerializerOptions())
                {
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            else
            {
                _logger.LogWarning("ContactDetails does not exist for Customer. Customer GUID: {CustomerId}", customerGuid);
                _logger.LogInformation("Function {FunctionName} has finished invoking", nameof(GetContactByIdHttpTrigger));
                return new NotFoundObjectResult($"Contact details ({contactGuid}) for customer ({customerGuid}) do not exist.");
            }
        }
    }
}

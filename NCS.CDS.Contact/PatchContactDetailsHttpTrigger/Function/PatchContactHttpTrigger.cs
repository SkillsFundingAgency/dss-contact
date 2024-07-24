using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using System.Text;

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function
{
    public class PatchContactHttpTrigger
    {
        private IResourceHelper _resourceHelper;
        private IHttpRequestHelper _httpRequestMessageHelper;
        private IValidate _validate;
        private IPatchContactDetailsHttpTriggerService _contactdetailsPatchService;
        private IDocumentDBProvider _provider;
        private ILogger logger;


        public PatchContactHttpTrigger(IResourceHelper resourceHelper,
             IHttpRequestHelper httpRequestMessageHelper,
             IValidate validate,
             IPatchContactDetailsHttpTriggerService contactdetailsPatchService,
             IDocumentDBProvider provider,
             ILogger<PatchContactHttpTrigger> logger)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _validate = validate;
            _contactdetailsPatchService = contactdetailsPatchService;
            _provider = provider;
            this.logger = logger;
        }


        [Function("PATCH")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customers/{customerId}/ContactDetails/{contactid}")] HttpRequest req, 
            string customerId, string contactid)
        {
            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                logger.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            var ApimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                logger.LogInformation("Unable to locate 'apimurl' in request header");
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            logger.LogInformation("C# HTTP trigger function Patch Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
            {
                logger.LogInformation($"No customer with ID [{customerGuid}]");
                return new BadRequestObjectResult(customerGuid);
            }

            if (!Guid.TryParse(contactid, out var contactGuid))
            {
                logger.LogInformation($"No contact with ID [{contactGuid}]");
                return new BadRequestObjectResult(contactGuid);
            }

            ContactDetailsPatch contactdetailsPatchRequest;

            try
            {
                contactdetailsPatchRequest = await _httpRequestMessageHelper.GetResourceFromRequest<ContactDetailsPatch>(req);
            }
            catch (JsonException ex)
            {
                logger.LogError($"Error: JsonException caught, UnprocessableEntity.");
                return new UnprocessableEntityObjectResult(ex);
            }

            if (contactdetailsPatchRequest == null)
                return new UnprocessableEntityObjectResult(req);

            contactdetailsPatchRequest.LastModifiedTouchpointId = touchpointId;

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
            {
                logger.LogInformation($"No customer with ID [{customerGuid}]");
                return new NoContentResult();
            }

            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
            {
                logger.LogInformation($"Customer with ID [{customerGuid}] is read only, operation forbidden.");
                return new ForbidResult(customerGuid.ToString());
            }

            var contactdetails = await _contactdetailsPatchService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

            if (contactdetails == null)
            {
                logger.LogInformation($"No contact with ID [{contactGuid}]");
                return new NoContentResult();
            }

            var errors = _validate.ValidateResource(contactdetailsPatchRequest, contactdetails, false);

            if (!string.IsNullOrEmpty(contactdetailsPatchRequest.EmailAddress))
            {
                var contacts = await _provider.GetContactsByEmail(contactdetailsPatchRequest.EmailAddress);
                if (contacts != null)
                {
                    foreach (var contact in contacts)
                    {
                        var isReadOnly = await _provider.DoesCustomerHaveATerminationDate(contact.CustomerId.GetValueOrDefault());
                        if (!isReadOnly && contact.CustomerId != contactdetails.CustomerId)
                        {
                            //if a customer that has the same email address is not readonly (has date of termination)
                            //then email address on the request cannot be used.
                            return new ConflictObjectResult(HttpStatusCode.Conflict);
                        }
                    }
                }
            }

            // Set Digital account properties so that contentenhancer can queue change on digital identity topic.
            var diaccount = await _provider.GetIdentityForCustomerAsync(contactdetails.CustomerId.Value);
            if (diaccount != null)
            {
                if (contactdetailsPatchRequest.EmailAddress == string.Empty)
                {
                    if (errors == null)
                        errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    errors.Add(new System.ComponentModel.DataAnnotations.ValidationResult("Email Address cannot be removed because it is associated with a Digital Account", new List<string>() { "EmailAddress" }));
                    return new UnprocessableEntityObjectResult(errors);
                }

                if (!string.IsNullOrEmpty(contactdetails.EmailAddress) && !string.IsNullOrEmpty(contactdetailsPatchRequest.EmailAddress) && contactdetails.EmailAddress?.ToLower() != contactdetailsPatchRequest.EmailAddress?.ToLower() && diaccount.IdentityStoreId.HasValue)
                {
                    contactdetails.SetDigitalAccountEmailChanged(contactdetailsPatchRequest.EmailAddress?.ToLower(), diaccount.IdentityStoreId.Value);
                }
            }

            if (errors != null && errors.Any())
                return new UnprocessableEntityObjectResult(errors);

            var updatedContactDetails = await _contactdetailsPatchService.UpdateAsync(contactdetails, contactdetailsPatchRequest);

            if (updatedContactDetails != null)
                await _contactdetailsPatchService.SendToServiceBusQueueAsync(updatedContactDetails, customerGuid, ApimURL);

            return updatedContactDetails == null ?
                new BadRequestObjectResult(contactGuid) :
               new OkObjectResult(JsonHelper.SerializeObject(updatedContactDetails));
        }

    }
}

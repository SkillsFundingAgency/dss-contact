using DFC.HTTP.Standard;
using DFC.Swagger.Standard.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

namespace NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function
{
    public class PatchContactHttpTrigger
    {
        private IResourceHelper _resourceHelper;
        private IHttpRequestHelper _httpRequestMessageHelper;
        private IValidate _validate;
        private IPatchContactDetailsHttpTriggerService _contactdetailsPatchService;
        private IDocumentDBProvider _provider;
        private readonly IHttpResponseMessageHelper _httpResponseMessageHelper;


        public PatchContactHttpTrigger(IResourceHelper resourceHelper,
             IHttpRequestHelper httpRequestMessageHelper,
             IValidate validate,
             IPatchContactDetailsHttpTriggerService contactdetailsPatchService,
             IDocumentDBProvider provider,
             IHttpResponseMessageHelper httpResponseMessageHelper)
        {
            _resourceHelper = resourceHelper;
            _httpRequestMessageHelper = httpRequestMessageHelper;
            _validate = validate;
            _contactdetailsPatchService = contactdetailsPatchService;
            _provider = provider;
            _httpResponseMessageHelper = httpResponseMessageHelper;
        }


        [FunctionName("PATCH")]
        [Response(HttpStatusCode = (int)HttpStatusCode.OK, Description = "Contact Details Patched", ShowSchema = true)]
        [Response(HttpStatusCode = (int)HttpStatusCode.NoContent, Description = "Resource Does Not Exist", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.BadRequest, Description = "Patch request is malformed", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Unauthorized, Description = "API Key unknown or invalid", ShowSchema = false)]
        [Response(HttpStatusCode = (int)HttpStatusCode.Forbidden, Description = "Insufficient Access To This Resource", ShowSchema = false)]
        [Response(HttpStatusCode = (int)422, Description = "Contact Details resource validation error(s)", ShowSchema = false)]
        [ProducesResponseType(typeof(ContactDetails), 200)]
        public async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "customers/{customerId}/ContactDetails/{contactid}")] HttpRequest req, ILogger log,
            string customerId, string contactid)
        {
            var touchpointId = _httpRequestMessageHelper.GetDssTouchpointId(req);
            if (string.IsNullOrEmpty(touchpointId))
            {
                log.LogInformation("Unable to locate 'TouchpointId' in request header.");
                return _httpResponseMessageHelper.BadRequest();
            }

            var ApimURL = _httpRequestMessageHelper.GetDssApimUrl(req);
            if (string.IsNullOrEmpty(ApimURL))
            {
                log.LogInformation("Unable to locate 'apimurl' in request header");
                return _httpResponseMessageHelper.BadRequest();
            }

            log.LogInformation("C# HTTP trigger function Patch Contact processed a request. " + touchpointId);

            if (!Guid.TryParse(customerId, out var customerGuid))
                return _httpResponseMessageHelper.BadRequest(customerGuid);

            if (!Guid.TryParse(contactid, out var contactGuid))
                return _httpResponseMessageHelper.BadRequest(contactGuid);

            ContactDetailsPatch contactdetailsPatchRequest;

            try
            {
                contactdetailsPatchRequest = await _httpRequestMessageHelper.GetResourceFromRequest<ContactDetailsPatch>(req);
            }
            catch (JsonException ex)
            {
                return _httpResponseMessageHelper.UnprocessableEntity(ex);
            }

            if (contactdetailsPatchRequest == null)
                return _httpResponseMessageHelper.UnprocessableEntity(req);

            contactdetailsPatchRequest.LastModifiedTouchpointId = touchpointId;

            var doesCustomerExist = await _resourceHelper.DoesCustomerExist(customerGuid);

            if (!doesCustomerExist)
                return _httpResponseMessageHelper.NoContent(customerGuid);

            var isCustomerReadOnly = await _resourceHelper.IsCustomerReadOnly(customerGuid);

            if (isCustomerReadOnly)
                return _httpResponseMessageHelper.Forbidden(customerGuid);

            var contactdetails = await _contactdetailsPatchService.GetContactDetailsForCustomerAsync(customerGuid, contactGuid);

            if (contactdetails == null)
                return _httpResponseMessageHelper.NoContent(contactGuid);

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
                            return _httpResponseMessageHelper.Conflict();
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
                    return _httpResponseMessageHelper.UnprocessableEntity(errors);
                }

                if (!string.IsNullOrEmpty(contactdetails.EmailAddress) && !string.IsNullOrEmpty(contactdetailsPatchRequest.EmailAddress) && contactdetails.EmailAddress?.ToLower() != contactdetailsPatchRequest.EmailAddress?.ToLower() && diaccount.IdentityStoreId.HasValue)
                {
                    contactdetails.SetDigitalAccountEmailChanged(contactdetailsPatchRequest.EmailAddress?.ToLower(), diaccount.IdentityStoreId.Value);
                }
                else
                    contactdetails.SetDigitalAccount();
            }

            if (errors != null && errors.Any())
                return _httpResponseMessageHelper.UnprocessableEntity(errors);

            var updatedContactDetails = await _contactdetailsPatchService.UpdateAsync(contactdetails, contactdetailsPatchRequest);

            if (updatedContactDetails != null)
                await _contactdetailsPatchService.SendToServiceBusQueueAsync(updatedContactDetails, customerGuid, ApimURL);

            return updatedContactDetails == null ?
                _httpResponseMessageHelper.BadRequest(contactGuid) :
                _httpResponseMessageHelper.Ok(JsonHelper.SerializeObject(updatedContactDetails));
        }

    }
}

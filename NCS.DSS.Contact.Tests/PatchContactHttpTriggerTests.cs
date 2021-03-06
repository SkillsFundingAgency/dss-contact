﻿using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PatchContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string ValidContactId = "1e1a555c-9633-4e12-ab28-09ed60d51cb3";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";
        private ILogger _log;
        private HttpRequestMessage _request;
        private IResourceHelper _resourceHelper;
        private IValidate _validate;
        private IHttpRequestMessageHelper _httpRequestMessageHelper;
        private IPatchContactDetailsHttpTriggerService _patchContactHttpTriggerService;
        private Models.ContactDetails _contactDetails;
        private Models.ContactDetailsPatch _contactDetailsPatch;
        private IDocumentDBProvider _provider;

        [SetUp]
        public void Setup()
        {
            _contactDetails = Substitute.For<Models.ContactDetails>();
            _contactDetailsPatch = Substitute.For<Models.ContactDetailsPatch>();

            _request = new HttpRequestMessage()
            {
                Content = new StringContent(string.Empty),
                RequestUri = 
                    new Uri($"http://localhost:7071/api/Customers/7E467BDB-213F-407A-B86A-1954053D3C24/ContactDetails/1e1a555c-9633-4e12-ab28-09ed60d51cb3")
            };

            _log = Substitute.For<ILogger>();
            _resourceHelper = Substitute.For<IResourceHelper>();
            _validate = Substitute.For<IValidate>();
            _httpRequestMessageHelper = Substitute.For<IHttpRequestMessageHelper>();
            _patchContactHttpTriggerService = Substitute.For<IPatchContactDetailsHttpTriggerService>();
            _provider = Substitute.For<IDocumentDBProvider>();
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns("0000000001");
            _httpRequestMessageHelper.GetApimURL(_request).Returns("http://localhost:7071/");
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange


            // Act
            var result = await RunFunction(InValidId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            // Arrange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            _validate.ValidateResource(Arg.Any<Models.ContactDetailsPatch>(), _contactDetails, false).Returns(validationResults);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)204, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Throws(new JsonException());
            
            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            // Arrange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(false);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenContactDoesNotExist()
        {
            // Arrange 
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToUpdateContactRecord()
        {
            // Arrange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusBadRequest_WhenRequestIsNotValid()
        {
            // Arange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult<Models.ContactDetails>(null).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsUnprocessableEntity_WhenEmptyPatchingEmailWithAnAssociatedDigitalEntity()
        {
            // Arange
            var patch = new ContactDetailsPatch() { EmailAddress="" };
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(patch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _provider.GetIdentityForCustomerAsync(Arg.Any<Guid>()).Returns(Task.FromResult(new DigitalIdentity() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult<Models.ContactDetails>(new ContactDetails()).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(422, (int)result.StatusCode);
        }


        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _provider.GetIdentityForCustomerAsync(Arg.Any<Guid>()).Returns(Task.FromResult<DigitalIdentity>(null));
            _provider.DoesContactDetailsWithEmailExistsForAnotherCustomer(Arg.Any<string>(), Arg.Any<Guid>()).Returns(Task.FromResult(false));
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult(_contactDetails).Result);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeConflict_WhenEmailAddressIsInUseByAnotherCustomer()
        {
            // Arrange
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _provider.GetIdentityForCustomerAsync(Arg.Any<Guid>()).Returns(Task.FromResult<DigitalIdentity>(null));
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress="test@test.com" }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult(_contactDetails).Result);
            _provider.GetContactsByEmail(Arg.Any<string>()).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() }));
            _provider.DoesCustomerHaveATerminationDate(Arg.Any<Guid>()).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOk_WhenEmailAddressIsInUseByAnotherCustomerThatIsTerminated()
        {
            // Arrange
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.GetContactDetailsFromRequest<Models.ContactDetailsPatch>(_request).Returns(Task.FromResult(_contactDetailsPatch).Result);
            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);
            _provider.GetIdentityForCustomerAsync(Arg.Any<Guid>()).Returns(Task.FromResult<DigitalIdentity>(null));
            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(new ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.UpdateAsync(Arg.Any<Models.ContactDetails>(), Arg.Any<Models.ContactDetailsPatch>()).Returns(Task.FromResult(_contactDetails).Result);
            _provider.GetContactsByEmail(Arg.Any<string>()).Returns(Task.FromResult<IList<ContactDetails>>(new List<ContactDetails>() { new ContactDetails() { CustomerId = Guid.NewGuid() } }));
            _provider.DoesCustomerHaveATerminationDate(Arg.Any<Guid>()).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, string contactDetailId)
        {
            return await PatchContactHttpTrigger.RunAsync(
                _request, _log, customerId, contactDetailId, _resourceHelper, _httpRequestMessageHelper, _validate, _patchContactHttpTriggerService, _provider).ConfigureAwait(false);
        }

    }
}
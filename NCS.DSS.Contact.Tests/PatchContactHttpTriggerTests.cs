using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
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
        private Mock<ILogger> _log;
        private HttpRequest _request;
        private Mock<IResourceHelper> _resourceHelper;
        private IValidate _validate;
        private Mock<IHttpRequestHelper> _httpRequestMessageHelper;
        private Mock<IPatchContactDetailsHttpTriggerService> _patchContactHttpTriggerService;
        private Models.ContactDetails _contactDetails;
        private Models.ContactDetailsPatch _contactDetailsPatch;
        private Mock<IDocumentDBProvider> _provider;
        private PatchContactHttpTrigger _function;
        private IHttpResponseMessageHelper _httpResponseMessageHelper;

        [SetUp]
        public void Setup()
        {
            _contactDetails = new Models.ContactDetails() { CustomerId = Guid.Parse(ValidCustomerId) };
            _contactDetailsPatch = new Models.ContactDetailsPatch();
            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _log = new Mock<ILogger>();
            _resourceHelper = new Mock<IResourceHelper>();
            _validate = new Validate();
            _httpRequestMessageHelper = new Mock<IHttpRequestHelper>();
            _patchContactHttpTriggerService = new Mock<IPatchContactDetailsHttpTriggerService>();
            _provider = new Mock<IDocumentDBProvider>();
            _httpResponseMessageHelper = new HttpResponseMessageHelper();
            _function = new PatchContactHttpTrigger(_resourceHelper.Object, _httpRequestMessageHelper.Object, _validate, _patchContactHttpTriggerService.Object, _provider.Object, _httpResponseMessageHelper);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x=>x.GetDssTouchpointId(_request)).Returns((string)null);

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x=>x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            var val = new Mock<IValidate>();
            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            val.Setup(x => x.ValidateResource(It.IsAny<Models.ContactDetailsPatch>(), _contactDetails, false)).Returns(validationResults);
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.ContactDetails>(_contactDetails));
            _function = new PatchContactHttpTrigger(_resourceHelper.Object, _httpRequestMessageHelper.Object, val.Object, _patchContactHttpTriggerService.Object, _provider.Object, _httpResponseMessageHelper);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(422, (int)result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenNoMobileNumberSuppliedForWhatsappPreferredContactMethod()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.PreferredContactMethod = ReferenceData.PreferredContactMethod.WhatsApp;
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.ContactDetails>(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(422, (int)result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Throws(new JsonException());
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult<Models.ContactDetails>(null));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=> x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(null));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(null));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var patch = new Models.ContactDetailsPatch() { EmailAddress = "" };
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(patch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(new Models.ContactDetails()));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _provider.Setup(x=>x.DoesContactDetailsWithEmailExistsForAnotherCustomer(It.IsAny<string>(), It.IsAny<Guid>())).Returns(Task.FromResult(false));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult(_contactDetails));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<Models.ContactDetails>>(new List<Models.ContactDetails>() { new Models.ContactDetails() }));
            _provider.Setup(x=> x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));

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
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<Models.ContactDetails>>(new List<Models.ContactDetails>() { new Models.ContactDetails() { CustomerId = Guid.NewGuid() } }));
            _provider.Setup(x=>x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, string contactDetailId)
        {
            return await _function.RunAsync(
                _request, _log.Object, customerId, contactDetailId).ConfigureAwait(false);
        }

    }
}
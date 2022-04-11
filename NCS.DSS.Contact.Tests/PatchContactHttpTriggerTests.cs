using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Helpers;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

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
            _httpRequestMessageHelper.GetTouchpointId(_request).Returns("0000000001");
            _httpRequestMessageHelper.GetApimURL(_request).Returns("http://localhost:7071/");
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x=>x.GetDssTouchpointId(_request)).Returns((string)null);
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenSubcontractorIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns((string)null);

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
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
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
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            var val = new Mock<IValidate>();
            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            _validate.ValidateResource(Arg.Any<Models.ContactDetailsPatch>(), false).Returns(validationResults);

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual((HttpStatusCode)422, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Throws(new JsonException());
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
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
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).Returns(false);

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
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
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
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=> x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(null));

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult<Models.ContactDetails>(_contactDetails).Result);

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusBadRequest_WhenRequestIsNotValid()
        {
            // Arange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(null));

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsNotValid()
        {
            // Arange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var patch = new Models.ContactDetailsPatch() { EmailAddress = "" };
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(patch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId) }));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult(new Models.DigitalIdentity() { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult<Models.ContactDetails>(new Models.ContactDetails()));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(_contactDetails).Result);

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

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<Models.ContactDetails>>(new List<Models.ContactDetails>() { new Models.ContactDetails() }));
            _provider.Setup(x=> x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            _resourceHelper.DoesCustomerExist(Arg.Any<Guid>()).ReturnsForAnyArgs(true);

            _patchContactHttpTriggerService.GetContactDetailsForCustomerAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(Task.FromResult(_contactDetails).Result);

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOk_WhenEmailAddressIsInUseByAnotherCustomerThatIsTerminated()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssSubcontractorId(_request)).Returns("9999999999");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x=>x.GetResourceFromRequest<Models.ContactDetailsPatch>(_request)).Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x=>x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x=>x.GetIdentityForCustomerAsync(It.IsAny<Guid>())).Returns(Task.FromResult<Models.DigitalIdentity>(null));
            _patchContactHttpTriggerService.Setup(x=>x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.FromResult(new Models.ContactDetails() { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.Setup(x=>x.UpdateAsync(It.IsAny<Models.ContactDetails>(), It.IsAny<Models.ContactDetailsPatch>())).Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x=>x.GetContactsByEmail(It.IsAny<string>())).Returns(Task.FromResult<IList<Models.ContactDetails>>(new List<Models.ContactDetails>() { new Models.ContactDetails() { CustomerId = Guid.NewGuid() } }));
            _provider.Setup(x=>x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.IsInstanceOf<HttpResponseMessage>(result);
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }

        private async Task<HttpResponseMessage> RunFunction(string customerId, string contactDetailId)
        {
            return await PatchContactHttpTrigger.RunAsync(
                _request, _log, customerId, contactDetailId, _resourceHelper, _httpRequestMessageHelper, _validate, _patchContactHttpTriggerService).ConfigureAwait(false);
        }

    }
}
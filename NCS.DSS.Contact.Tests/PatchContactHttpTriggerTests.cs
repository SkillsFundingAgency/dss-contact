using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Function;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.ReferenceData;
using NCS.DSS.Contact.Validation;
using Newtonsoft.Json;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class PatchContactHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string ValidContactId = "1e1a555c-9633-4e12-ab28-09ed60d51cb3";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";

        private Mock<IPatchContactDetailsHttpTriggerService> _patchContactHttpTriggerService;
        private Mock<IResourceHelper> _resourceHelper;
        private Mock<IHttpRequestHelper> _httpRequestMessageHelper;
        private Mock<ILogger<PatchContactHttpTrigger>> _logger;
        private Mock<IDocumentDBProvider> _provider;
        private Mock<IConvertToDynamic> _convertToDynamic;

        private ContactDetails _contactDetails;
        private ContactDetailsPatch _contactDetailsPatch;
        private HttpRequest _request;
        private IValidate _validate;
        private PatchContactHttpTrigger _function;

        [SetUp]
        public void Setup()
        {
            _patchContactHttpTriggerService = new Mock<IPatchContactDetailsHttpTriggerService>();
            _httpRequestMessageHelper = new Mock<IHttpRequestHelper>();
            _resourceHelper = new Mock<IResourceHelper>();
            _provider = new Mock<IDocumentDBProvider>();
            _convertToDynamic = new Mock<IConvertToDynamic>();
            _logger = new Mock<ILogger<PatchContactHttpTrigger>>();

            _contactDetails = new ContactDetails { CustomerId = Guid.Parse(ValidCustomerId) };
            _contactDetailsPatch = new ContactDetailsPatch();
            _validate = new Validate();
            _request = new DefaultHttpContext().Request;

            _function = new PatchContactHttpTrigger(_patchContactHttpTriggerService.Object,
                _httpRequestMessageHelper.Object,
                _resourceHelper.Object,
                _provider.Object,
                _validate,
                _convertToDynamic.Object,
                _logger.Object);
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");

            // Act
            var result = await RunFunction(InValidId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactHasFailedValidation()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            var val = new Mock<IValidate>();
            var validationResults = new List<ValidationResult> { new ValidationResult("contactDetail Id is Required") };
            val.Setup(x => x.ValidateResource(It.IsAny<ContactDetailsPatch>(), _contactDetails, false))
                .Returns(validationResults);
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(_contactDetails));
            _function = new PatchContactHttpTrigger(_patchContactHttpTriggerService.Object,
                _httpRequestMessageHelper.Object,
                _resourceHelper.Object,
                _provider.Object,
                val.Object,
                _convertToDynamic.Object,
                _logger.Object);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenNoMobileNumberSuppliedForWhatsappPreferredContactMethod()
        {
            //Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.PreferredContactMethod = PreferredContactMethod.WhatsApp;
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeUnprocessableEntity_WhenContactRequestIsInvalid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request)).Throws(new JsonException());
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeNoContent_WhenContactDoesNotExist()
        {
            // Arrange 
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeBadRequest_WhenUnableToUpdateContactRecord()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(),
                    It.IsAny<ContactDetailsPatch>()))
                .Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusBadRequest_WhenRequestIsNotValid()
        {
            // Arange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(), It.IsAny<ContactDetailsPatch>())).Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsUnprocessableEntity_WhenEmptyPatchingEmailWithAnAssociatedDigitalEntity()
        {
            // Arange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            var patch = new ContactDetailsPatch { EmailAddress = "" };
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request)).Returns(Task.FromResult(patch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId) }));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(new DigitalIdentity { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(),
                    It.IsAny<ContactDetailsPatch>()))
                .Returns(Task.FromResult(new ContactDetails()));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<UnprocessableEntityObjectResult>());
        }


        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<DigitalIdentity>(null));
            _provider
                .Setup(x => x.DoesContactDetailsWithEmailExistsForAnotherCustomer(It.IsAny<string>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(false));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId) }));
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(),
                    It.IsAny<ContactDetailsPatch>()))
                .Returns(Task.FromResult(_contactDetails));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeConflict_WhenEmailAddressIsInUseByAnotherCustomer()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<DigitalIdentity>(null));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(
                    Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(),
                    It.IsAny<ContactDetailsPatch>()))
                .Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails() }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<ConflictObjectResult>());
        }

        [Test]
        public async Task PatchContactHttpTrigger_ReturnsStatusCodeOk_WhenEmailAddressIsInUseByAnotherCustomerThatIsTerminated()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.EmailAddress = "test@test.com";
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<DigitalIdentity>(null));
            _patchContactHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(
                    Task.FromResult(new ContactDetails { CustomerId = new Guid(ValidCustomerId), EmailAddress = "test@test.com" }));
            _patchContactHttpTriggerService
                .Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(), It.IsAny<ContactDetailsPatch>()))
                .Returns(Task.FromResult(_contactDetails));
            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails { CustomerId = Guid.NewGuid() } }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(true));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        }

        [Test]
        public async Task PatchContactContactPrefHttpTrigger_ReturnsStatusCodeOK_WhenRequestIsValid()
        {
            // Arrange
            _httpRequestMessageHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _httpRequestMessageHelper.Setup(x => x.GetDssApimUrl(_request)).Returns("http://localhost:7071/");
            _contactDetailsPatch.PreferredContactMethod = PreferredContactMethod.Email;
            _contactDetailsPatch.MobileNumber = "07553788901";
            _httpRequestMessageHelper.Setup(x => x.GetResourceFromRequest<ContactDetailsPatch>(_request))
                .Returns(Task.FromResult(_contactDetailsPatch));
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _provider.Setup(x => x.GetIdentityForCustomerAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult<DigitalIdentity>(null));
            _patchContactHttpTriggerService.Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(),
                    It.IsAny<Guid>()))
                .Returns(Task.FromResult(new ContactDetails
                {
                    CustomerId = new Guid(ValidCustomerId),
                    EmailAddress = "test@test.com"
                }));

            // Capture the updated contact details
            ContactDetails updatedContactDetails = null;
            _patchContactHttpTriggerService.Setup(x => x.UpdateAsync(It.IsAny<ContactDetails>(), It.IsAny<ContactDetailsPatch>()))
                                           .Callback<ContactDetails, ContactDetailsPatch>((contact, patch) =>
                                           {
                                               contact.PreferredContactMethod = patch.PreferredContactMethod; // Ensure the patch is applied
                                               updatedContactDetails = contact;
                                           })
                                           .Returns(Task.FromResult(_contactDetails));

            _provider.Setup(x => x.GetContactsByEmail(It.IsAny<string>())).Returns(
                Task.FromResult<IList<ContactDetails>>(new List<ContactDetails> { new ContactDetails() }));
            _provider.Setup(x => x.DoesCustomerHaveATerminationDate(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(updatedContactDetails.PreferredContactMethod,
                Is.EqualTo(PreferredContactMethod.Email),
                $"Expected: {PreferredContactMethod.Email}, But was: {updatedContactDetails?.PreferredContactMethod}");
        }

        private async Task<IActionResult> RunFunction(string customerId, string contactDetailId)
        {
            return await _function.RunAsync(
                _request, customerId, contactDetailId).ConfigureAwait(false);
        }
    }
}
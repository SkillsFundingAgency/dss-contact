﻿using System;
using System.Net;
using System.Threading.Tasks;
using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Function;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.Models;
using NUnit.Framework;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class GetContactByIdHttpTriggerTests
    {
        private const string ValidCustomerId = "7E467BDB-213F-407A-B86A-1954053D3C24";
        private const string ValidContactId = "1e1a555c-9633-4e12-ab28-09ed60d51cb3";
        private const string InValidId = "1111111-2222-3333-4444-555555555555";

        private Mock<IGetContactDetailsByIdHttpTriggerService> _getContactByIdHttpTriggerService;
        private Mock<IHttpRequestHelper> _httpRequestHelper;
        private Mock<IResourceHelper> _resourceHelper;
        private Mock<ILogger<GetContactByIdHttpTrigger>> _logger;
        
        private GetContactByIdHttpTrigger _function;
        private ContactDetails _contact;
        private HttpRequest _request;


        [SetUp]
        public void Setup()
        {
            _contact = new ContactDetails();
            _request = new DefaultHttpContext().Request;
            _resourceHelper = new Mock<IResourceHelper>();
            _httpRequestHelper = new Mock<IHttpRequestHelper>();
            _getContactByIdHttpTriggerService = new Mock<IGetContactDetailsByIdHttpTriggerService>();
            _logger = new Mock<ILogger<GetContactByIdHttpTrigger>>();
            _function = new GetContactByIdHttpTrigger(
                _getContactByIdHttpTriggerService.Object,
                _httpRequestHelper.Object,
                _resourceHelper.Object,
                _logger.Object);
        }

        [Test]
        public async Task GetContacByIdHttpTrigger_ReturnsStatusCodeBadRequest_WhenTouchpointIdIsNotProvided()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns((string)null);

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetContactByIdHttpTrigger_ReturnsStatusCodeBadRequest_WhenCustomerIdIsInvalid()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");

            // Act
            var result = await RunFunction(InValidId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetContactByIdHttpTrigger_ReturnsStatusCodeBadRequest_WhenContactIdIsInvalid()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");

            // Act
            var result = await RunFunction(ValidCustomerId, InValidId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task GetContactByIdHttpTrigger_ReturnsStatusCodeNoContent_WhenCustomerDoesNotExist()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(false));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task GetContactByIdHttpTrigger_ReturnsStatusCodeNoContent_WhenContactDoesNotExist()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _getContactByIdHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _logger.Object))
                .Returns(Task.FromResult<ContactDetails>(null));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task GetContactByIdHttpTrigger_ReturnsStatusCodeOk_WhenContactExists()
        {
            // Arrange
            _httpRequestHelper.Setup(x => x.GetDssTouchpointId(_request)).Returns("0000000001");
            _resourceHelper.Setup(x => x.DoesCustomerExist(It.IsAny<Guid>())).Returns(Task.FromResult(true));
            _getContactByIdHttpTriggerService
                .Setup(x => x.GetContactDetailsForCustomerAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), _logger.Object))
                .Returns(Task.FromResult(_contact));

            // Act
            var result = await RunFunction(ValidCustomerId, ValidContactId);
            var responseResult = result as JsonResult;

            //Assert
            Assert.That(responseResult, Is.InstanceOf<JsonResult>());
            Assert.That(responseResult.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        }

        private async Task<IActionResult> RunFunction(string customerId, string contactDetailsId)
        {
            return await _function.RunAsync(_request, customerId, contactDetailsId).ConfigureAwait(false);
        }
    }
}

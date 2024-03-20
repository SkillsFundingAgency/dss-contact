using DFC.HTTP.Standard;
using Microsoft.AspNetCore.Http;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.ReferenceData;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace NCS.DSS.Contact.Tests
{
    [TestFixture]
    public class HttpRequestHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIsEmptyString()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Unknown, contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsNull_WhenPreferredContactMethodValueIsNotProvided()
        {
            // Arrange
            var json = "{}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.IsNull(contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsEmail_WhenPreferredContactMethodValueIs1AsString()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"1\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Email, contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsEmail_WhenPreferredContactMethodValueIs1AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 1}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Email, contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIsAllAlphabets()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": \"abc\"}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Unknown, contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsPost_WhenPreferredContactMethodValueIs5AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 5}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Post, contactdetailsPatchRequest.PreferredContactMethod);
        }

        [Test]
        public async Task GetResourceFromRequest_SetsPreferredContactMethodAsUnknown_WhenPreferredContactMethodValueIs8AsInteger()
        {
            // Arrange
            var json = "{\"PreferredContactMethod\": 6}";
            var request = GetHttpRequest(json);

            // Act
            var helper = new HttpRequestHelper();
            var contactdetailsPatchRequest = await helper.GetResourceFromRequest<ContactDetailsPatch>(request);

            // Assert
            Assert.IsInstanceOf<ContactDetailsPatch>(contactdetailsPatchRequest);
            Assert.AreEqual(PreferredContactMethod.Unknown, contactdetailsPatchRequest.PreferredContactMethod);
        }

        private static HttpRequest GetHttpRequest(string json)
        {
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Body = memoryStream;
            request.ContentType = "application/json";
            return request;
        }
    }
}
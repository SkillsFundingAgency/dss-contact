﻿using DFC.Swagger.Standard;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Mvc;
namespace NCS.DSS.Contact.APIDefinition
{
    public class GenerateCustomerSwaggerDoc
    {
        public const string APITitle = "ContactDetails";
        public const string APIDefinitionName = "API-Definition";
        public const string APIDefRoute = APITitle + "/" + APIDefinitionName;
        public const string APIDescription = "Basic details of a National Careers Service " + APITitle + " Resource";
        public const string ApiDefRoute = APITitle + "/" + APIDefinitionName;
        private readonly ISwaggerDocumentGenerator _swaggerDocumentGenerator;
        public const string ApiVersion = "1.0.0";

        public GenerateCustomerSwaggerDoc(ISwaggerDocumentGenerator swaggerDocumentGenerator)
        {
            _swaggerDocumentGenerator = swaggerDocumentGenerator;
        }

        [Function(APIDefinitionName)]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiDefRoute)] HttpRequest req)
        {
            var swagger = _swaggerDocumentGenerator.GenerateSwaggerDocument(req, APITitle, APIDescription,
                APIDefinitionName, ApiVersion, Assembly.GetExecutingAssembly());

            if (string.IsNullOrEmpty(swagger))
                return new NoContentResult();

            return new OkObjectResult(new StringContent(swagger));
        }
    }
}
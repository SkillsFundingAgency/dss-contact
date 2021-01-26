using DFC.HTTP.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NCS.DSS.Contact;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;
[assembly: FunctionsStartup(typeof(Startup))]
namespace NCS.DSS.Contact
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<IGetContactHttpTriggerService, GetContactHttpTriggerService>();
            builder.Services.AddTransient<IGetContactDetailsByIdHttpTriggerService, GetContactDetailsByIdHttpTriggerService>();
            builder.Services.AddTransient<IPostContactDetailsHttpTriggerService, PostContactDetailsHttpTriggerService>();
            builder.Services.AddTransient<IPatchContactDetailsHttpTriggerService, PatchContactDetailsHttpTriggerService>();
            builder.Services.AddTransient<IDocumentDBProvider, DocumentDBProvider>();
            builder.Services.AddTransient<IResourceHelper, ResourceHelper>();
            builder.Services.AddTransient<IValidate, Validate>();
            builder.Services.AddTransient<IHttpRequestHelper, HttpRequestHelper>();
            builder.Services.AddSingleton<IHttpResponseMessageHelper, HttpResponseMessageHelper>();
            builder.Services.AddSingleton<ISwaggerDocumentGenerator, SwaggerDocumentGenerator>();

        }
    }
}

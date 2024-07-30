using DFC.HTTP.Standard;
using DFC.Swagger.Standard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddLogging();
        services.AddTransient<IDocumentDBProvider, DocumentDBProvider>();
        services.AddTransient<IGetContactHttpTriggerService, GetContactHttpTriggerService>();
        services.AddTransient<IGetContactDetailsByIdHttpTriggerService, GetContactDetailsByIdHttpTriggerService>();
        services.AddTransient<IPostContactDetailsHttpTriggerService, PostContactDetailsHttpTriggerService>();
        services.AddTransient<IPatchContactDetailsHttpTriggerService, PatchContactDetailsHttpTriggerService>();
        services.AddTransient<IResourceHelper, ResourceHelper>();
        services.AddTransient<IValidate, Validate>();
        services.AddTransient<IHttpRequestHelper, HttpRequestHelper>();
        services.AddSingleton<ISwaggerDocumentGenerator, SwaggerDocumentGenerator>();
    })
    .Build();

host.Run();

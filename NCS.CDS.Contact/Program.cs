using DFC.HTTP.Standard;
using DFC.Swagger.Standard;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCS.DSS.Contact.Cosmos.Helper;
using NCS.DSS.Contact.Cosmos.Provider;
using NCS.DSS.Contact.GetContactDetailsByIdHttpTrigger.Service;
using NCS.DSS.Contact.GetContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Models;
using NCS.DSS.Contact.PatchContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service;
using NCS.DSS.Contact.Validation;

namespace NCS.DSS.Contact
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

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
                    services.AddSingleton<IConvertToDynamic, ConvertToDynamic>();

                    services.Configure<LoggerFilterOptions>(options =>
                    {
                        // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
                        // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
                        LoggerFilterRule toRemove = options.Rules.FirstOrDefault(rule => rule.ProviderName
                            == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
                        if (toRemove is not null)
                        {
                            options.Rules.Remove(toRemove);
                        }
                    });
                })
                .Build();

            await host.RunAsync();
        }
    }
}

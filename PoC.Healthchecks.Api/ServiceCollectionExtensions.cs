using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PoC.Healthchecks.Api;

public static class ServiceCollectionExtensions
{
    public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(configuration["ConnectionStrings:SqlServer"], healthQuery: "select 1",
                name: "SQL Server", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Database" })
            .AddRedis(configuration["ConnectionStrings:Redis"], tags: new[] { "Cache" })
            .AddRabbitMQ(rabbitConnectionString: configuration["ConnectionStrings:RabbitMQ"], name: "RabbitMQ 1", tags: new[] { "Messaging" })
            .AddRabbitMQ(rabbitConnectionString: configuration["ConnectionStrings:RabbitMQ2"], name: "RabbitMQ 2", tags: new[] { "Messaging" })
            ;

        //services.AddHealthChecksUI();
        services.AddHealthChecksUI(opt =>
            {
                opt.SetEvaluationTimeInSeconds(10); //time in seconds between check    
                opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
                opt.SetApiMaxActiveRequests(1); //api requests concurrency    
                opt.AddHealthCheckEndpoint("Healthchecks PoC", configuration["Healtchecks:PollingUri"]); //map health check api    

                //bypass ssl check for the PoC, do not do this in production!!
                opt.UseApiEndpointHttpMessageHandler(sp =>
                {
                    return new HttpClientHandler
                    {
                        ClientCertificateOptions = ClientCertificateOption.Manual,
                        ServerCertificateCustomValidationCallback =
                            (httpRequestMessage, cert, cetChain, policyErrors) =>
                            {
                                return true;
                            }
                    };
                });

                opt.AddWebhookNotification("Wehbook.site", 
                    uri: "https://webhook.site/770b450e-5620-4b0b-b8ff-2fe444519af3",
                    payload: "{ message: \"Webhook report for [[LIVENESS]]: [[FAILURE]] - Description: [[DESCRIPTIONS]]\"}",
                    restorePayload: "{ message: \"[[LIVENESS]] is back to life\"}");

                opt.AddWebhookNotification("Teams",
                    uri: "https://webhook.site/770b450e-5620-4b0b-b8ff-2fe444519af3",
                    payload: "{ message: \"Webhook report for [[LIVENESS]]: [[FAILURE]] - Description: [[DESCRIPTIONS]]\"}",
                    restorePayload: "{ message: \"[[LIVENESS]] is back to life\"}");

            })
            .AddInMemoryStorage();
    }
}
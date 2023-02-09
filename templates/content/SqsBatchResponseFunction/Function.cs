using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqsBatchResponseFunction;

/*
    Deriving the Function class from RequestResponseFunction<SQSEvent, SQSBatchResponse>
    enables partial batch support by catching message handler exceptions and reporting
    them to AWS in the SQSBatchResponse, rather then letting them fly.

    *** For this to work properly, be sure to enable partial batch support on the AWS side,
    by turning on "Report batch item failures" on the Event Source! Refer to the
    README ("Handling SQS messages with partial batch responses" section) for further
    information. ***
*/
public class Function : RequestResponseFunction<SQSEvent, SQSBatchResponse>
{
    protected override void Configure(IConfigurationBuilder builder)
    {
        // Use this method to register your configuration flow. Exactly like in ASP.NET Core
    }

    protected override void ConfigureLogging(ILoggingBuilder logging, IExecutionEnvironment executionEnvironment)
    {
        // Use this method to install logger providers
    }

    protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
    {
        // You need this line to register your handler
        services.UseQueueMessageHandler<TestMessage, TestMessageHandler>();

        // Use this method to register your services. Exactly like in ASP.NET Core
    }
}

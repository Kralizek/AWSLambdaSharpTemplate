using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using NUnit.Framework;


namespace Tests.Lambda;

[TestFixture]
public class CustomSerializerTests
{
    private const string RawMessage = "hello world";
    private const string Message = "hello world top secret injection from serializer";
    
    [SetUp]
    public void Initialize()
    {
            
    }

    private class SomeEvent
    {
        public string message { get; set; }
    }

    private class CustomSerializer : IMessageSerializer
    {
        public T Deserialize<T>(string input)
        {
            // do some fancy 'serializing'
            // use input parameter instead of the hardcoded value here
            return JsonSerializer.Deserialize<T>($"{{\r\n\"message\": \"{Message}\"\r\n}}")!;
        }
    }

    // Handlers
    private class DummyHandler : IMessageHandler<SomeEvent>
    {
        public Task HandleAsync(SomeEvent evt, ILambdaContext context)
        {
            Assert.That(evt?.message, Is.EqualTo(Message));

            return Task.CompletedTask;
        }
    }

    private class DummyHandlerNoChanges : IMessageHandler<SomeEvent>
    {
        public Task HandleAsync(SomeEvent evt, ILambdaContext context)
        {
            Assert.That(evt?.message, Is.EqualTo(RawMessage));
            return Task.CompletedTask;
        }
    }

    // Functions
    private class DummyFunction : EventFunction<SQSEvent>
    {
        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
        {
            services.UseQueueMessageHandler<SomeEvent, DummyHandler>();
                
            services.AddSingleton<IMessageSerializer, CustomSerializer>();
        }
    }

    private class DummyFunctionNoChanges : EventFunction<SQSEvent>
    {
        protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
        {
            services.UseQueueMessageHandler<SomeEvent, DummyHandlerNoChanges>();
        }
    }

    [Test]
    public async Task HandleAsync_DeserializesCorrectly()
    {
        var instance = new DummyFunction();

        await instance.FunctionHandlerAsync(new SQSEvent()
        {
            Records = new List<SQSEvent.SQSMessage>()
            {
                new SQSEvent.SQSMessage()
                {
                    // or xml.. or json.. or whatever you want
                    Body = $"{{\r\n\"message\": \"{RawMessage}\"\r\n}}"
                }
            }
        }, null!);
    }

    [Test]
    public async Task HandleAsync_DeserializesCorrectly_WithoutChanges()
    {
        var instance = new DummyFunctionNoChanges();
        await instance.FunctionHandlerAsync(new SQSEvent()
        {
            Records = new List<SQSEvent.SQSMessage>()
            {
                new SQSEvent.SQSMessage()
                {
                    // or xml.. or json.. or whatever you want
                    Body = $"{{\r\n\"message\": \"{RawMessage}\"\r\n}}"
                }
            }
        }, null!);
    }
}
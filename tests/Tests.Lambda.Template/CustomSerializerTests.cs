using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using NUnit.Framework;


namespace Tests.Lambda
{
    [TestFixture]
    public class CustomSerializerTests
    {
        [SetUp]
        public void Initialize()
        {
            
        }

        private class SomeEvent
        {
            public string message { get; set; }
        }

        private class CustomSerializer : ISerializer
        {
            public T Deserialize<T>(string input)
            {
                // do some fancy 'serializing'
                // use input parameter instead of the hardcoded value here
                return JsonSerializer.Deserialize<T>("{\r\n\"message\": \"hello world topsecret injection from serializer\"\r\n}");
            }
        }

        // Handlers
        private class DummyHandler : IMessageHandler<SomeEvent>
        {
            public Task HandleAsync(SomeEvent evt, ILambdaContext context)
            {
                Assert.True(evt.message == "hello world topsecret injection from serializer");
                return Task.CompletedTask;
            }
        }

        private class DummyHandlerNoChanges : IMessageHandler<SomeEvent>
        {
            public Task HandleAsync(SomeEvent evt, ILambdaContext context)
            {
                Assert.True(evt.message == "hello world");
                return Task.CompletedTask;
            }
        }

        // Functions
        private class DummyFunction : EventFunction<SQSEvent>
        {
            protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
            {
                services.UseSqsHandler<SomeEvent, DummyHandler>(serializer: new CustomSerializer());
            }
        }

        private class DummyFunctionNoChanges : EventFunction<SQSEvent>
        {
            protected override void ConfigureServices(IServiceCollection services, IExecutionEnvironment executionEnvironment)
            {
                services.UseSqsHandler<SomeEvent, DummyHandlerNoChanges>();
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
                        Body = "{\r\n\"message\": \"hello world\"\r\n}"
                    }
                }
            }, null);
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
                        Body = "{\r\n\"message\": \"hello world\"\r\n}"
                    }
                }
            }, null);
        }
    }
}

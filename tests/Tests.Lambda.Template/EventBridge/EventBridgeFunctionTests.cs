using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.CloudWatchEvents;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda.EventBridge;

[TestFixture]
public class EventBridgeFunctionTests
{
    [SetUp]
    public void SetUp() => TrackingHandler.Reset();

    [Test]
    public async Task FunctionHandlerAsync_passes_typed_event_and_context_to_handler()
    {
        var sut = new TestEventBridgeFunction();
        var lambdaContext = TestLambdaContexts.Create();
        lambdaContext.AwsRequestId = "request-id";
        var input = new CloudWatchEvent<OrderCreated>
        {
            Id = "event-id",
            Source = "com.example.orders",
            DetailType = "Order Created",
            Detail = new OrderCreated("order-123", 42.50m)
        };

        await sut.FunctionHandlerAsync(input, lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(TrackingHandler.ReceivedEvent, Is.SameAs(input));
            Assert.That(TrackingHandler.ReceivedEvent?.Detail.OrderId, Is.EqualTo("order-123"));
            Assert.That(TrackingHandler.ReceivedEvent?.Source, Is.EqualTo("com.example.orders"));
            Assert.That(TrackingHandler.ReceivedEvent?.DetailType, Is.EqualTo("Order Created"));
            Assert.That(TrackingHandler.ReceivedContext?.AwsRequestId, Is.EqualTo("request-id"));
            Assert.That(TrackingHandler.ReceivedContext?.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    public sealed class TestEventBridgeFunction : EventBridgeFunction<OrderCreated, TrackingHandler>;

    public sealed record OrderCreated(string OrderId, decimal Total);

    public sealed class TrackingHandler : IEventBridgeHandler<OrderCreated>
    {
        public static CloudWatchEvent<OrderCreated>? ReceivedEvent { get; private set; }
        public static EventContext? ReceivedContext { get; private set; }

        public static void Reset()
        {
            ReceivedEvent = null;
            ReceivedContext = null;
        }

        public ValueTask HandleAsync(
            CloudWatchEvent<OrderCreated> input,
            EventContext context,
            CancellationToken cancellationToken)
        {
            ReceivedEvent = input;
            ReceivedContext = context;
            return ValueTask.CompletedTask;
        }
    }
}
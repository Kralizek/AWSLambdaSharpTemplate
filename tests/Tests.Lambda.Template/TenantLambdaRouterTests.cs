using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using Kralizek.Lambda;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class TenantLambdaRouterTests
{
    [Test]
    public async Task RouteAsync_invokes_target_synchronously_with_tenant_and_payload()
    {
        InvokeRequest? capturedRequest = null;

        var lambda = new Mock<IAmazonLambda>();
        lambda
            .Setup(client => client.InvokeAsync(
                It.IsAny<InvokeRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<InvokeRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new InvokeResponse());

        var services = new ServiceCollection();
        services.AddSingleton(lambda.Object);
        services.AddTenantLambdaRouting();

        await using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<ITenantLambdaRouter>();

        await sut.RouteAsync(
            TenantLambdaRoute.Utf8(
                "tenant-42",
                "orders-processor",
                "{\"orderId\":\"123\"}")
            with
            {
                Qualifier = "production"
            });

        Assert.That(capturedRequest, Is.Not.Null);

        using var reader = new StreamReader(capturedRequest!.Payload, Encoding.UTF8, leaveOpen: true);
        capturedRequest.Payload.Position = 0;
        var payload = await reader.ReadToEndAsync();

        Assert.Multiple(() =>
        {
            Assert.That(capturedRequest.FunctionName, Is.EqualTo("orders-processor"));
            Assert.That(capturedRequest.TenantId, Is.EqualTo("tenant-42"));
            Assert.That(capturedRequest.Qualifier, Is.EqualTo("production"));
            Assert.That(capturedRequest.InvocationType, Is.EqualTo(InvocationType.RequestResponse));
            Assert.That(payload, Is.EqualTo("{\"orderId\":\"123\"}"));
        });
    }

    [Test]
    public void RouteAsync_surfaces_downstream_function_errors()
    {
        var lambda = new Mock<IAmazonLambda>();
        lambda
            .Setup(client => client.InvokeAsync(
                It.IsAny<InvokeRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse
            {
                FunctionError = "Unhandled"
            });

        var services = new ServiceCollection();
        services.AddSingleton(lambda.Object);
        services.AddTenantLambdaRouting();

        using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<ITenantLambdaRouter>();
        var route = TenantLambdaRoute.Utf8("tenant-42", "orders-processor", "{}");

        Assert.That(
            async () => await sut.RouteAsync(route),
            Throws.TypeOf<TenantLambdaInvocationException>()
                .With.Property(nameof(TenantLambdaInvocationException.FunctionName)).EqualTo("orders-processor")
                .And.Property(nameof(TenantLambdaInvocationException.TenantId)).EqualTo("tenant-42")
                .And.Property(nameof(TenantLambdaInvocationException.FunctionError)).EqualTo("Unhandled"));
    }

    [Test]
    public async Task RouteAsync_emits_outgoing_faas_span_without_adding_tenant_to_metrics()
    {
        Activity? stoppedActivity = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LambdaTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stoppedActivity = activity
        };
        ActivitySource.AddActivityListener(listener);

        var lambda = new Mock<IAmazonLambda>();
        lambda
            .Setup(client => client.InvokeAsync(
                It.IsAny<InvokeRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvokeResponse());

        var services = new ServiceCollection();
        services.AddSingleton(lambda.Object);
        services.AddTenantLambdaRouting();

        await using var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<ITenantLambdaRouter>();

        await sut.RouteAsync(
            TenantLambdaRoute.Utf8(
                "tenant-42",
                "orders-processor",
                "{}"));

        Assert.That(stoppedActivity, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(stoppedActivity!.DisplayName, Is.EqualTo("orders-processor"));
            Assert.That(stoppedActivity.Kind, Is.EqualTo(ActivityKind.Client));
            Assert.That(stoppedActivity.GetTagItem("faas.invoked_name"), Is.EqualTo("orders-processor"));
            Assert.That(stoppedActivity.GetTagItem("faas.invoked_provider"), Is.EqualTo("aws"));
            Assert.That(stoppedActivity.GetTagItem("kralizek.aws.lambda.tenant_id"), Is.EqualTo("tenant-42"));
        });
    }
}

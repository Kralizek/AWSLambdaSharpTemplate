using System.Collections.Generic;
using System.Linq;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class FunctionContextTests
{
    [Test]
    public void Factory_populates_strongly_typed_metadata_and_preserves_lambda_context()
    {
        var lambdaContext = TestLambdaContexts.Create();

        var context = FunctionContextFactory.CreateEventContext(lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(context.AwsRequestId, Is.EqualTo(lambdaContext.AwsRequestId));
            Assert.That(context.FunctionName, Is.EqualTo(lambdaContext.FunctionName));
            Assert.That(context.FunctionVersion, Is.EqualTo(lambdaContext.FunctionVersion));
            Assert.That(context.InvokedFunctionArn, Is.EqualTo(lambdaContext.InvokedFunctionArn));
            Assert.That(context.MemoryLimitInMB, Is.EqualTo(lambdaContext.MemoryLimitInMB));
            Assert.That(context.RemainingTime, Is.EqualTo(lambdaContext.RemainingTime));
            Assert.That(context.LogGroupName, Is.EqualTo(lambdaContext.LogGroupName));
            Assert.That(context.LogStreamName, Is.EqualTo(lambdaContext.LogStreamName));
            Assert.That(context.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Factory_root_properties_are_immutable_and_fully_enumerable()
    {
        var lambdaContext = TestLambdaContexts.Create();

        var context = FunctionContextFactory.CreateRequestContext(lambdaContext);

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties, Is.Not.AssignableTo<IDictionary<string, object?>>());
            Assert.That(context.Properties.Count, Is.EqualTo(1));
            Assert.That(context.Properties.Values.Single(), Is.SameAs(lambdaContext));
            Assert.That(context.Properties.Single().Value, Is.SameAs(lambdaContext));
            Assert.That(context.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Factory_root_properties_match_dictionary_null_key_behavior()
    {
        var context = FunctionContextFactory.CreateRequestContext(TestLambdaContexts.Create());

        Assert.Multiple(() =>
        {
            Assert.That(() => { _ = context.Properties[null!]; }, Throws.ArgumentNullException);
            Assert.That(() => { _ = context.Properties.ContainsKey(null!); }, Throws.ArgumentNullException);
            Assert.That(() => { _ = context.Properties.TryGetValue(null!, out _); }, Throws.ArgumentNullException);
        });
    }

    [Test]
    public void Semantic_contexts_can_be_specialized_without_accepting_lambda_context()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);

        Assert.That(new SpecializedEventContext(metadata, properties).GetLambdaContext(), Is.SameAs(lambdaContext));
        Assert.That(new SpecializedRequestContext(metadata, properties).GetLambdaContext(), Is.SameAs(lambdaContext));
        Assert.That(new SpecializedRecordContext(metadata, properties).GetLambdaContext(), Is.SameAs(lambdaContext));
    }

    [Test]
    public void Specialized_context_snapshots_extensible_property_bag()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);
        properties["Source"] = "SQS";

        var context = new SpecializedRecordContext(metadata, properties);

        properties["Source"] = "Changed";
        properties["Later"] = true;

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties["Source"], Is.EqualTo("SQS"));
            Assert.That(context.Properties.ContainsKey("Later"), Is.False);
            Assert.That(context.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Derived_context_overlay_exposes_new_property_and_preserves_existing_properties()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);
        properties["Source"] = "SQS";

        var source = new SpecializedRecordContext(metadata, properties);
        var context = new OverlaidRecordContext(source, "Record", 42);

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties["Record"], Is.EqualTo(42));
            Assert.That(context.Properties.TryGetValue("Record", out var record), Is.True);
            Assert.That(record, Is.EqualTo(42));
            Assert.That(context.Properties["Source"], Is.EqualTo("SQS"));
            Assert.That(context.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Derived_context_overlay_replaces_existing_property_without_duplicating_it()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);
        properties["Source"] = "SQS";
        properties["Other"] = 42;

        var source = new SpecializedRecordContext(metadata, properties);
        var context = new OverlaidRecordContext(source, "Source", "Overlaid");

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties.Count, Is.EqualTo(source.Properties.Count));
            Assert.That(context.Properties["Source"], Is.EqualTo("Overlaid"));
            Assert.That(context.Properties.TryGetValue("Source", out var sourceValue), Is.True);
            Assert.That(sourceValue, Is.EqualTo("Overlaid"));
            Assert.That(context.Properties["Other"], Is.EqualTo(42));
            Assert.That(context.Properties.Keys.Count(key => key == "Source"), Is.EqualTo(1));
            Assert.That(context.Properties.Count(pair => pair.Key == "Source"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Derived_context_two_property_overlay_exposes_both_properties_without_nesting()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);
        properties["Source"] = "S3";

        var source = new SpecializedRecordContext(metadata, properties);
        var context = new TwoPropertyOverlaidRecordContext(
            source,
            "Request",
            "request",
            "Task",
            "task");

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties.Count, Is.EqualTo(source.Properties.Count + 2));
            Assert.That(context.Properties["Request"], Is.EqualTo("request"));
            Assert.That(context.Properties["Task"], Is.EqualTo("task"));
            Assert.That(context.Properties["Source"], Is.EqualTo("S3"));
            Assert.That(context.Properties.Keys.Count(key => key == "Request"), Is.EqualTo(1));
            Assert.That(context.Properties.Keys.Count(key => key == "Task"), Is.EqualTo(1));
            Assert.That(context.GetLambdaContext(), Is.SameAs(lambdaContext));
        });
    }

    [Test]
    public void Derived_context_two_property_overlay_handles_source_and_overlay_key_collisions()
    {
        var lambdaContext = TestLambdaContexts.Create();
        var metadata = FunctionContextFactory.CreateMetadata(lambdaContext);
        var properties = FunctionContextFactory.CreateProperties(lambdaContext);
        properties["Request"] = "old";
        properties["Other"] = 42;

        var source = new SpecializedRecordContext(metadata, properties);
        var context = new TwoPropertyOverlaidRecordContext(
            source,
            "Request",
            "first",
            "Request",
            "second");

        Assert.Multiple(() =>
        {
            Assert.That(context.Properties.Count, Is.EqualTo(source.Properties.Count));
            Assert.That(context.Properties["Request"], Is.EqualTo("second"));
            Assert.That(context.Properties.TryGetValue("Request", out var value), Is.True);
            Assert.That(value, Is.EqualTo("second"));
            Assert.That(context.Properties.Keys.Count(key => key == "Request"), Is.EqualTo(1));
            Assert.That(context.Properties.Count(pair => pair.Key == "Request"), Is.EqualTo(1));
        });
    }

    private sealed class SpecializedEventContext : EventContext
    {
        public SpecializedEventContext(
            FunctionContextMetadata metadata,
            IReadOnlyDictionary<string, object?> properties)
            : base(metadata, properties) { }
    }

    private sealed class SpecializedRequestContext : RequestContext
    {
        public SpecializedRequestContext(
            FunctionContextMetadata metadata,
            IReadOnlyDictionary<string, object?> properties)
            : base(metadata, properties) { }
    }

    private sealed class SpecializedRecordContext : RecordContext
    {
        public SpecializedRecordContext(
            FunctionContextMetadata metadata,
            IReadOnlyDictionary<string, object?> properties)
            : base(metadata, properties) { }
    }

    private sealed class OverlaidRecordContext : RecordContext
    {
        public OverlaidRecordContext(
            RecordContext source,
            string propertyName,
            object? propertyValue)
            : base(source, propertyName, propertyValue) { }
    }

    private sealed class TwoPropertyOverlaidRecordContext : RecordContext
    {
        public TwoPropertyOverlaidRecordContext(
            RecordContext source,
            string firstPropertyName,
            object? firstPropertyValue,
            string secondPropertyName,
            object? secondPropertyValue)
            : base(
                source,
                firstPropertyName,
                firstPropertyValue,
                secondPropertyName,
                secondPropertyValue)
        { }
    }
}

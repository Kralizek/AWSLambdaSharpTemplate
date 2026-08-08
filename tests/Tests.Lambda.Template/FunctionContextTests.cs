using Amazon.Lambda.Core;

using Kralizek.Lambda;

using NUnit.Framework;

namespace Tests.Lambda;

[TestFixture]
public class FunctionContextTests
{
    [Test]
    public void Semantic_contexts_can_be_specialized_from_another_assembly()
    {
        var lambdaContext = TestLambdaContexts.Create();

        Assert.That(new SpecializedEventContext(lambdaContext).LambdaContext, Is.SameAs(lambdaContext));
        Assert.That(new SpecializedRequestContext(lambdaContext).LambdaContext, Is.SameAs(lambdaContext));
        Assert.That(new SpecializedRecordContext(lambdaContext).LambdaContext, Is.SameAs(lambdaContext));
    }

    private sealed class SpecializedEventContext : EventContext
    {
        public SpecializedEventContext(ILambdaContext lambdaContext)
            : base(lambdaContext) { }
    }

    private sealed class SpecializedRequestContext : RequestContext
    {
        public SpecializedRequestContext(ILambdaContext lambdaContext)
            : base(lambdaContext) { }
    }

    private sealed class SpecializedRecordContext : RecordContext
    {
        public SpecializedRecordContext(ILambdaContext lambdaContext)
            : base(lambdaContext) { }
    }
}
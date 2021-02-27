using AutoFixture.Idioms;
using Kralizek.Lambda;
using NUnit.Framework;

namespace Tests.Lambda
{
    [TestFixture]
    public class LambdaConfiguratorTests
    {
        [Test, CustomAutoData]
        public void Constructor_is_guarded(GuardClauseAssertion assertion) => assertion.Verify(typeof (LambdaConfigurator).GetConstructors());

        [Test, CustomAutoData]
        public void Properties_are_configured_from_constructor_parameters(WritablePropertyAssertion assertion) => assertion.Verify(typeof (LambdaConfigurator));
    }
}

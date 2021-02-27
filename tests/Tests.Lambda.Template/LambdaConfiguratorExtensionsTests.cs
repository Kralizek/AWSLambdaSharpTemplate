using Kralizek.Lambda;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Lambda
{
    [TestFixture]
    public class LambdaConfiguratorExtensionsTests
    {
        [Test, CustomAutoData]
        public void UseCustomSerializer_registers_serializer_type(LambdaConfigurator configurator)
        {
            configurator.UseCustomSerializer<SystemTextJsonSerializer>();

            var sp = configurator.Services.BuildServiceProvider();

            var serializer = sp.GetService<ISerializer>();

            Assert.That(serializer, Is.InstanceOf<SystemTextJsonSerializer>());
        }
    }
}

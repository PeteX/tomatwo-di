using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Tomatwo.DependencyInjection;

namespace DependencyInjectionTest
{
    public class Tests
    {
        private IServiceProvider serviceProvider;

        [SetUp]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<InjectionService>()
                .AddSingleton<InjectThisService>()
                .AddEnhancedServiceProvider();

            serviceProvider = serviceCollection.BuildServiceProvider();
        }

        [Test]
        public void TestPropertyInjection()
        {
            var service = serviceProvider.GetService<InjectionService>();
            Assert.AreEqual("Hello World!", service.GetPropertyMessage());
        }

        [Test]
        public void TestFieldInjection()
        {
            var service = serviceProvider.GetService<InjectionService>();
            Assert.AreEqual("Hello World!", service.GetFieldMessage());
        }
    }
}

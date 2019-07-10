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
                .AddSingleton<InterceptionService>();

            serviceCollection.AddEnhancedServiceProvider(provider =>
            {
                provider.AddInterceptor<SimpleInterceptAttribute>(interception =>
                {
                    return 2;
                });

                provider.AddInterceptor<InterceptAttribute>(interception =>
                {
                    InterceptionService interceptionService = (InterceptionService)interception.Target;
                    string baseMessage = (string)interception.Invoke(interception.Target, interception.Args);
                    return baseMessage + interceptionService.ExclamationMark;
                });
            });

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

        [Test]
        public void TestBasicInterception()
        {
            var service = serviceProvider.GetService<InterceptionService>();
            Assert.AreEqual(2, service.Simple());
        }

        [Test]
        public void TestInterception()
        {
            var service = serviceProvider.GetService<InterceptionService>();
            Assert.AreEqual("Hello there Pete 2!!", service.MakeMessage("there", "Pete", 2));
        }

        [Test]
        public void TestNonVirtualTarget()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<NonVirtualTarget>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                serviceCollection.AddEnhancedServiceProvider(provider =>
                {
                    provider.AddInterceptor<SimpleInterceptAttribute>(interception => 2);
                });
            });
        }

        [Test]
        public void TestOneAtATime()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<OneAtATime>();

            Assert.Throws<InvalidOperationException>(() =>
            {
                serviceCollection.AddEnhancedServiceProvider(provider =>
                {
                    provider.AddInterceptor<InterceptAttribute>(interception => 2);
                    provider.AddInterceptor<SimpleInterceptAttribute>(interception => 2);
                });
            });
        }
    }
}

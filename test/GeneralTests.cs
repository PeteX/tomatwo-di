using System;
using Microsoft.AspNetCore.Mvc;
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
                .AddSingleton<InterceptionService>()
                .AddSingleton<AttributesService>();

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

        [Test]
        public void TestAttributeClassWithoutParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var attributes = service.GetType().GetCustomAttributes(typeof(ApiControllerAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            ApiControllerAttribute attribute = attributes[0] as ApiControllerAttribute;
            Assert.IsNotNull(attribute);
        }

        [Test]
        public void TestAttributeClassWithParameters()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var attributes = service.GetType().GetCustomAttributes(typeof(RouteAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            RouteAttribute attribute = (RouteAttribute)attributes[0];
            Assert.AreEqual("class-attr", attribute.Template);
            Assert.AreEqual(345, attribute.Order);
        }

        [Test]
        public void TestAttributeMethodWithoutParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var method = service.GetType().GetMethod("MethodWithoutParameter");
            var attributes = method.GetCustomAttributes(typeof(HttpGetAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            HttpGetAttribute attribute = (HttpGetAttribute)attributes[0];
            Assert.IsNull(attribute.Template);
        }

        [Test]
        public void TestAttributeMethodWithParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var method = service.GetType().GetMethod("MethodWithParameter");
            var attributes = method.GetCustomAttributes(typeof(HttpGetAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            HttpGetAttribute attribute = (HttpGetAttribute)attributes[0];
            Assert.AreEqual("httpget", attribute.Template);
        }

        [Test]
        public void TestAttributeMethodWithNamedParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var method = service.GetType().GetMethod("MethodWithNamedParameter");
            var attributes = method.GetCustomAttributes(typeof(HttpGetAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            HttpGetAttribute attribute = (HttpGetAttribute)attributes[0];
            Assert.AreEqual("param", attribute.Template);
            Assert.AreEqual(123, attribute.Order);
        }

        [Test]
        public void TestAttributeArgWithoutParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var method = service.GetType().GetMethod("ArgWithoutParameter");
            var arg = method.GetParameters()[0];
            var attributes = arg.GetCustomAttributes(typeof(FromHeaderAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            FromHeaderAttribute attribute = (FromHeaderAttribute)attributes[0];
            Assert.IsNull(attribute.Name);
        }

        [Test]
        public void TestAttributeArgWithNamedParameter()
        {
            var service = serviceProvider.GetService<AttributesService>();
            var method = service.GetType().GetMethod("ArgWithNamedParameter");
            var arg = method.GetParameters()[0];
            var attributes = arg.GetCustomAttributes(typeof(FromHeaderAttribute), false);
            Assert.AreEqual(1, attributes.Length);
            FromHeaderAttribute attribute = (FromHeaderAttribute)attributes[0];
            Assert.AreEqual("header", attribute.Name);
        }

        [Test]
        public void TestMultipleRegistrations()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<InjectionService>()
                .AddSingleton<ITestInterface, InjectionService>()
                .AddSingleton<InjectThisService>();

            serviceCollection.AddEnhancedServiceProvider();
            serviceProvider = serviceCollection.BuildServiceProvider();

            var service1 = serviceProvider.GetService<InjectionService>();
            Assert.AreEqual("Hello World!", service1.GetFieldMessage());
            var service2 = serviceProvider.GetService<ITestInterface>();
            Assert.AreEqual("Hello World!", service2.GetPropertyMessage());
        }
    }
}

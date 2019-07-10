using System;

namespace DependencyInjectionTest
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SimpleInterceptAttribute : Attribute
    {
    }
}

using System;
using System.Reflection;

namespace Tomatwo.DependencyInjection
{
    public struct Interception
    {
        public object Target;
        public object[] Args;
        public Func<object, object[], object> Invoke;
        public MethodInfo Method;
    }
}

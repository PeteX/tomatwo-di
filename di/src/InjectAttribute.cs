using System;

namespace Tomatwo.DependencyInjection
{
    /// <summary>
    /// Mark a property or field as an injection target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
    }
}

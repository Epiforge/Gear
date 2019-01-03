using System;

namespace Gear.Components
{
    /// <summary>
    /// Specifies that the attributed code should be excluded from code coverage information
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
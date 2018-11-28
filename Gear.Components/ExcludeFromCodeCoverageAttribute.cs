using System;

namespace Gear.Components
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
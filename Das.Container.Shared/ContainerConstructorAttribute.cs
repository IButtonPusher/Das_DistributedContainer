using System;

namespace Das.Container.Shared
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ContainerConstructorAttribute : Attribute
    {
    }
}

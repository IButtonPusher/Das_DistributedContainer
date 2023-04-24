using System;
using System.Threading.Tasks;

namespace Das.Container;

[AttributeUsage(AttributeTargets.Constructor)]
// ReSharper disable once ClassNeverInstantiated.Global
public class ContainerConstructorAttribute : Attribute
{
}
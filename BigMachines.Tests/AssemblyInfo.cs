using System;
using System.Runtime.CompilerServices;
using Tinyhand;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]

internal sealed class EmptyTestServiceProvider : IServiceProvider
{
    [ModuleInitializer]
    internal static void Initialize() => TinyhandSerializer.ServiceProvider = new EmptyTestServiceProvider();

    public object? GetService(Type serviceType) => null;
}

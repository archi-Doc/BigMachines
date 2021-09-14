// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BigMachines;

namespace Advanced;

public class SomeService
{
    public void Print() => Console.WriteLine("Some service");
}

// Machine depends on SomeService.
[MachineObject(0x4f8f7256)]
public partial class ServiceProviderMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.TryCreate<ServiceProviderMachine.Interface>(0);
    }

    public ServiceProviderMachine(BigMachine<int> bigMachine, SomeService service)
        : base(bigMachine)
    {
        this.Service = service;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(3));
    }

    public SomeService Service { get; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.Service.Print();
        return StateResult.Continue;
    }
}

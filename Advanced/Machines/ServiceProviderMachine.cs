// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BigMachines;

namespace Advanced;

/*public class SomeService
{
    public void Print(string? text) => Console.WriteLine($"Some service : {text}");
}

// Machine depends on SomeService.
[MachineObject(0x4f8f7256)]
public partial class ServiceProviderMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.CreateOrGet<ServiceProviderMachine.Interface>(0, "A"); // Create a machine and set a parameter.
    }

    public ServiceProviderMachine(BigMachine<int> bigMachine, SomeService service)
        : base(bigMachine)
    {
        this.Service = service;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(3));
    }

    protected override void SetParameter(object? createParam)
    {// Receives a parameter. Note that this method is NOT called during deserialization.
        this.Text = (string?)createParam;
    }

    public SomeService Service { get; }

    public string? Text { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.Service.Print(this.Text);
        return StateResult.Continue;
    }
}*/

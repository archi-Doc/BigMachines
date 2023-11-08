// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

public class SomeService
{
    public void Print(string? text) => Console.WriteLine($"Some service : {text}");
}

// Machine depends on SomeService.
[MachineObject(UseServiceProvider = true)]
public partial class ServiceProviderMachine : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.ServiceProviderMachine.GetOrCreate(0, "A"); // Create a machine and set a parameter.
    }

    public ServiceProviderMachine(SomeService service)
        : base()
    {
        this.Service = service;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.Lifespan = TimeSpan.FromSeconds(3);
    }

    protected override void OnCreate(object? createParam)
    {// Receives the parameter at the time of creation. Note that it is not called during deserialization.
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
}

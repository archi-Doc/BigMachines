using System;
using Arc.Threading;
using BigMachines;

namespace Playground;

[MachineObject]
internal partial class TinyMachine : Machine
{
    public TinyMachine()
        : base()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Tiny machine: {this.count++}");
        if (this.count > 4)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }

    [CommandMethod]
    protected CommandResult<int> Command1(int x)
        => new(CommandResult.Success, x + 2);

    protected override void OnTerminate()
    {
        Console.WriteLine($"Tiny machine: Terminated");
        ThreadCore.Root.Terminate();
    }

    private int count;
}

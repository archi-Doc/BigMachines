using System;
using BigMachines;

[MachineObject]
internal partial class GlobalNamespaceMachine : Machine
{
    public GlobalNamespaceMachine()
        : base()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        return StateResult.Terminate;
    }
}

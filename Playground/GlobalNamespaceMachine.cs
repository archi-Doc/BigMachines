using System;
using BigMachines;

[MachineObject]
internal partial class GlobalNamespaceMachine : Machine
{
    public GlobalNamespaceMachine()
        : base()
    {
        this.DefaultInterval = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        return StateResult.Terminate;
    }
}

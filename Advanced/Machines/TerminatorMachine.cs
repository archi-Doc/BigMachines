// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject(StartByDefault = true)]
public partial class TerminatorMachine : Machine
{
    public TerminatorMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        /*if (!((IBigMachine)this.BigMachine).CheckActiveMachine(typeof(TerminatorMachine)))
        {
            Console.WriteLine("Terminate2 (no machine)");
            ThreadCore.Root.Terminate(); // Terminate the application thread.
            return StateResult.Terminate;
        }*/

        foreach (var x in this.BigMachine.GetArray())
        {
            if (x.MachineInformation.MachineType != typeof(TerminatorMachine) && x.Count > 0)
            {
                foreach (var y in x.GetArray())
                {
                    if (y.IsActive == true)
                    {
                        return StateResult.Continue;
                    }
                }
            }
        }

        if (((IBigMachine)this.BigMachine).GetExceptionCount() > 0)
        {// Remaining exceptions.
            return StateResult.Continue;
        }

        Console.WriteLine("Terminate (no machine)");
        ThreadCore.Root.Terminate(); // Terminate the application thread.
        return StateResult.Terminate;
    }
}

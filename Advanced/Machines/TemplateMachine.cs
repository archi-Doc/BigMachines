// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

/*[MachineObject]
public partial class TemplateMachine : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
    }

    public TemplateMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Template ({this.Identifier.ToString()}) - {this.Count++}");
        if (this.Count > 5)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}*/

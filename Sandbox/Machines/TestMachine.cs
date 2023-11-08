// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

namespace Sandbox;

[BigMachineObject(Inclusive = true)]
[AddMachine<ParentClass.TinyMachine2<int>>()]
public partial class TestBigMachine { }

[MachineObject]
public partial class CommandTestMachine : Machine<int>
{
    [CommandMethod(All = true)]
    public CommandResult CommandParam(string name, int age)
        => CommandResult.Success;

    [CommandMethod(All = true)]
    public CommandResult<int> CommandResponse(int x)
        => new(CommandResult.Success, x + 2);

    [CommandMethod(WithLock = false, All = true)]
    public CommandResult CommandNoLock()
        => CommandResult.Success;

    [CommandMethod(All = true)]
    public async Task<CommandResult<int>> TaskCommandResponse(int x)
        => new(CommandResult.Success, x + 3);

    [CommandMethod(All = true)]
    public async Task<CommandResult> TaskCommandParam(string name, int age)
        => CommandResult.Success;
}

[MachineObject]
public partial class AccessibilityMachine : Machine
{
    [StateMethod(0)]
    public StateResult PublicState(StateParameter parameter)
        => StateResult.Terminate;

    [StateMethod]
    protected StateResult ProtectedState(StateParameter parameter)
        => StateResult.Terminate;

    [StateMethod]
    private StateResult PrivateState(StateParameter parameter)
        => StateResult.Terminate;
}

/*[MachineObject]
public partial class AccessibilityMachine2 : AccessibilityMachine
{// PrivateState() is currently not supported.
    public AccessibilityMachine2()
        : base()
    {
    }
}*/

[MachineObject(UseServiceProvider = true)]
public partial class NoDefaultConstructorMachine : Machine
{
    public NoDefaultConstructorMachine(IServiceProvider serviceProvider)
        : base()
    {
    }

    [StateMethod(0)]
    public StateResult PublicState(StateParameter parameter)
        => StateResult.Terminate;
}

public partial class ParentClass
{
    [MachineObject]
    public partial class TinyMachine : Machine
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

        protected override void OnTermination()
        {
            Console.WriteLine($"Tiny machine: Terminated");
            ThreadCore.Root.Terminate();
        }

        private int count;
    }

    [TinyhandObject]
    [MachineObject(Control = MachineControlKind.Single)]
    public partial class TinyMachine2<TData> : Machine
    {
        public TinyMachine2()
            : base()
        {
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
            => StateResult.Terminate;

        [StateMethod]
        public StateResult Second(StateParameter parameter)
            => StateResult.Terminate;

        [StateMethod]
        public async Task<StateResult> Third(StateParameter parameter)
            => StateResult.Terminate;
    }
}

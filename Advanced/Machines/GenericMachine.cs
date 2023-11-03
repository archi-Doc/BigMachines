// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Advanced;

#pragma warning disable SA1403

namespace BigMachines
{
    [BigMachineObject(Comprehensive = true)]
    [AddMachine<Advanced.TestMachine>]
    [AddMachine<GenericMachine<string>>]
    public partial class BigMachine;
}

namespace Advanced
{
    [MachineObject]
    public partial class GenericMachine<TData> : Machine<int>
    {
        public static void Test(BigMachine bigMachine, TData data)
        {
            // The machine will run at regular intervals (1 second).
            var machine = bigMachine.GenericMachine.GetOrCreate(0, data);
        }

        public GenericMachine()
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
        }

        public int Count { get; set; }

        public TData Data { get; set; } = default!;

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"GenericMachine({this.Data?.ToString()}): Initial - {this.Count++}");
            return StateResult.Continue;
        }

        protected override void OnCreation(object? createParam)
        {
            this.Data = (TData)createParam!;
        }
    }

    /*[MachineObject]
    public partial class GenericMachine2<TData> : Machine<TData>
        where TData : notnull
    {
        public static void Test(BigMachine bigMachine, string data)
        {
            // The machine will run at regular intervals (1 second).
            var machine = bigMachine.GenericMachine2.GetOrCreate(data);
        }

        public GenericMachine2()
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
        }

        public int Count { get; set; }

        public TData Data { get; set; } = default!;

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"GenericMachine({this.Data?.ToString()}): Initial - {this.Count++}");
            return StateResult.Continue;
        }

        protected override void OnCreation(object? createParam)
        {
            this.Data = (TData)createParam!;
        }
    }*/
}

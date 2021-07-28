// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace ConsoleApp1
{
    public class SomeService
    {
        public void Print() => Console.WriteLine("Some service");
    }

    // Machine depends on SomeService.
    [MachineObject(0x4f8f7256)]
    public partial class ServiceProviderMachine : Machine<int>
    {
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
}

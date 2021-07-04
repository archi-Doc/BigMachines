using System;
using Arc.Threading;
using BigMachines;
using DryIoc;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            /*var container = new Container();
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Singleton);

            var machine = container.Resolve<TestMachine>();*/

            var bigMachine = new BigMachine<int>(ThreadCore.Root);
            var machine = new TestMachine(bigMachine);
            bigMachine.Add(machine);

            ThreadCore.Root.Terminate();
            ThreadCore.Root.WaitForTermination(2000);
        }
    }
}

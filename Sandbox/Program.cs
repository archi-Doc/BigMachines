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
            bigMachine.TryAdd<TestMachine>(3, null);
            bigMachine.TryAdd<TestMachine>(1, null);
            bigMachine.AddMachine<TestMachine>(3, null);
            bigMachine.GetMachine<TestMachine, TestMachine.State>(33);
            var mmi = bigMachine.GetMachine<TestMachine>(33);
            if (mmi != null)
            {
                mmi.Value
            }

            ThreadCore.Root.Terminate();
            ThreadCore.Root.WaitForTermination(2000);
        }
    }
}

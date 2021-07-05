using System;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

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

            BigMachine<int>.InterfaceTypeToFunc[typeof(TestMachine.Interface)] = x => new TestMachine(x);

            var bigMachine = new BigMachine<int>(ThreadCore.Root);
            var machine = new TestMachine(bigMachine);
            var testMachine = bigMachine.TryGet<TestMachine.Interface>(3);
            testMachine = bigMachine.Create<TestMachine.Interface>(3, null);
            testMachine = bigMachine.Create<TestMachine.Interface>(3, null);
            testMachine = bigMachine.Create<TestMachine.Interface>(3, null, true);
            var testMachine2 = bigMachine.TryGet<ManMachineInterface>(3);
            if (testMachine != null)
            {
                var b = testMachine.ChangeStateTwoWay(TestMachine.State.First);
                if (testMachine.GetCurrentState() == TestMachine.State.First)
                {
                }
            }

            var bb = bigMachine.Serialize();
            bigMachine.Deserialize(bb);

            ThreadCore.Root.Terminate();
            ThreadCore.Root.WaitForTermination(2000);
        }
    }
}

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

            // typeof(TestMachine.Interface) => GroupInfo ( Constructor, TypeId, typeof(TestMachine) )
            BigMachine<int>.StaticInfo[typeof(TestMachine.Interface)] = new(typeof(TestMachine), 0, x => new TestMachine(x));

            /*var container = new Container();
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Singleton);
            var bigMachine = container.Resolve<BigMachine<int>>();*/
            // var container_machine = container.Resolve<TestMachine>();

            var bigMachine = new BigMachine<int>(ThreadCore.Root);
            var testMachine = bigMachine.TryGet<TestMachine.Interface>(3);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            if (testMachine != null)
            {
                var b = testMachine.ChangeStateTwoWay(TestMachine.State.First);
                if (testMachine.GetCurrentState() == TestMachine.State.First)
                {
                }
            }

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>();

            var bb = bigMachine.Serialize();
            bigMachine.Deserialize(bb);

            ThreadCore.Root.Wait(10000, 10);

            ThreadCore.Root.Terminate();
            ThreadCore.Root.WaitForTermination(2000);
        }
    }
}

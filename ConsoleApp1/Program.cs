using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

namespace ConsoleApp1
{
    [TinyhandObject(UseServiceProvider = true)] // Annotate TinyhandObject attribute and set UseServiceProvider to true if you want enable serialization.
    [StateMachine(0)] // Annotate StateMachine and set Machine type id (unique number).
    public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier>
    {
        public TestMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.IsSerializable = true; // Serialize machine
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Time interval for machine processing.
            this.SetLifespan(TimeSpan.FromSeconds(10)); // Time until the machine automatically terminates.
        }

        [Key(10)]
        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {// Inside lock (machine) statement.
            Console.WriteLine($"TestMachine {this.Identifier}: Initial");

            this.ChangeState(TestMachine.State.One);
            return StateResult.Continue;
        }

        [StateMethod(1)]
        protected StateResult One(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: One");

            this.ChangeState(State.Two);

            return StateResult.Continue;
        }

        protected bool TwoCanEnter()
        {
            var result = this.Count++ > 2;
            if (result)
            {
                Console.WriteLine("One -> Two approved.");
            }
            else
            {
                Console.WriteLine("One -> Two denied.");

            }

            return result;
        }

        [StateMethod(2)]
        protected StateResult Two(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Two");
            this.SetTimeout(TimeSpan.FromSeconds(0.5));

            return StateResult.Continue;
        }

        protected override void OnTerminate()
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Terminated");

            ThreadCore.Root.Terminate();
            // this.BigMachine.Core.Terminate();
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
            {// Console window is closing or process terminated.
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
                ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
            };

            Console.CancelKeyPress += (s, e) =>
            {// Ctrl+C pressed
                e.Cancel = true;
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            };

            var container = new Container(); // You can use DI container if you want.
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Transient);
            var bigMachine = container.Resolve<BigMachine<int>>();

            // Load
            /*try
            {
                using (var fs = new FileStream("app.data", FileMode.Open))
                {
                    var bs = new byte[fs.Length];
                    fs.Read(bs);
                    bigMachine.Deserialize(bs);
                }
            }
            catch
            {
            }*/

            var testMachine = bigMachine.TryCreate<TestMachine.Interface>(3); // Machine is created using the interface class, not the machine class itself.
            if (testMachine != null)
            {
                var currentState = testMachine.GetCurrentState(); // Get current state. You can operate machines using the interface class.
            }

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>();

            testMachine = testGroup.TryGet<TestMachine.Interface>(3);

            var testMachine2 = bigMachine.Create<TestMachine.Interface>(4);

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.

            // Save
            /*var data = bigMachine.Serialize();
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }

            bigMachine.Deserialize(data);*/

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }

        internal static void Other()
        {

        }
    }
}

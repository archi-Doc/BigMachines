using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

namespace QuickStart
{
    [StateMachine(0)] // Annotate StateMachine and set Machine type id (unique number).
    public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class. The type of an identifier is int.
    {
        public TestMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine processing.
            this.SetLifespan(TimeSpan.FromSeconds(5)); // Time until the machine automatically terminates.
        }

        public int Count { get; set; }

        [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
        protected StateResult Initial(StateParameter parameter)
        {// Inside lock (machine) statement.
            Console.WriteLine($"TestMachine {this.Identifier}: Initial");
            this.ChangeState(TestMachine.State.One); // Change to state One (The name of method becomes the state name).
            return StateResult.Continue;
        }

        [StateMethod(1)]
        protected StateResult One(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: One - {this.Count++}");
            return StateResult.Continue;
        }

        protected override void OnTerminated()
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Terminated");
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var bigMachine = new BigMachine<int>(ThreadCore.Root);

            var testMachine = bigMachine.TryCreate<TestMachine.Interface>(3); // Machine is created via the interface class and identifier, not the machine class itself.
            if (testMachine != null)
            {
                var currentState = testMachine.GetCurrentState(); // Get current state. You can operate machines using the interface class.
            }

            testMachine = bigMachine.TryGet<TestMachine.Interface>(3); // Get the created machine.

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>(); // Group is a collection of machines.
            testMachine = testGroup.TryGet<TestMachine.Interface>(3); // Same as above

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.
        }
    }
}

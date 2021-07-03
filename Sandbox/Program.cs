using System;
using Arc.Threading;
using BigMachines;

namespace Sandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var bigMachine = new BigMachine<int>(ThreadCore.Root);
            var machine = new TestMachine();
        }
    }
}

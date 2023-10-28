﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.Design;

internal class CommandDesign8
{// Task.Run + ManualResetEventSlim
    internal class Command
    {
        public Command(bool flag)
        {
            this.Flag = flag;
        }

        public bool Flag { get; set; }
    }

    internal const int N = 1000_000;
    internal const int MillisecondInterval = 5;

    // private static object obj = new();
    private static ConcurrentQueue<Command> concurrentQueue = new();
    private static CancellationTokenSource cancellationTokenSource = new();
    private static CancellationToken cancellationToken;
    private static ManualResetEventSlim manualEvent2 = new(false); // Just slow
    // private static bool EventFlag = false;

    internal static async Task Test()
    {
        cancellationToken = cancellationTokenSource.Token;
        var sw = new Stopwatch();

        Start("Event");
        await TestCommand();
        Stop();

        Start("Event Parallel");
        await TestCommandParallel();
        Stop();

        Start("Event TwoWay");
        await TestCommandTwoWay();
        Stop();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        Console.WriteLine();

        void Start(string name)
        {
            Console.Write($"{name, -25}: ");
            sw.Restart();
        }

        void Stop()
        {
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
        }

        void Stop2()
        {
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedTicks} ticks");
        }
    }

    internal static async Task TestCommand()
    {
        for (var i = 0; i < N; i++)
        {
            Send(1);
        }
    }

    internal static async Task TestCommandParallel()
    {
        Parallel.For(0, N, x =>
        {
            Send(1);
        });
    }

    internal static async Task TestCommandTwoWay()
    {
        for (var i = 0; i < N; i++)
        {
            await SendTwoWay();
        }
    }

    internal static void Send(int n)
    {
        concurrentQueue.Enqueue(new Command(false));

        Task.Run(ReceiveAction);

        return;
    }

    internal static async Task SendTwoWay()
    {
        var c = new Command(false);
        concurrentQueue.Enqueue(c);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(ReceiveAction);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        while (true)
        {
            if (c.Flag)
            {
                return;
            }

            if (manualEvent2.Wait(5))
            {
                manualEvent2.Reset();
            }
        }
    }

    internal static void ReceiveAction()
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        while (concurrentQueue.TryDequeue(out var command))
        {
            command.Flag = true;
        }

        manualEvent2.Set();
    }
}

// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Test;

#pragma warning disable SA1401 // Fields should be private
    public class LoopChecker
    {
        public const int InitialArray = 4;

        public static AsyncLocal<LoopChecker> AsyncLocalInstance = new();

        public LoopChecker()
        {
            this.RunId = new uint[InitialArray];
            this.CommandId = new uint[InitialArray];
        }

        public LoopChecker(LoopChecker loopChecker)
        {
            this.RunId = new uint[loopChecker.RunId.Length];
            this.RunIdCount = loopChecker.RunIdCount;
            for (var n = 0; n < loopChecker.RunIdCount; n++)
            {
                this.RunId[n] = loopChecker.RunId[n];
            }

            this.CommandId = new uint[loopChecker.CommandId.Length];
            this.CommandIdCount = loopChecker.CommandIdCount;
            for (var n = 0; n < loopChecker.CommandIdCount; n++)
            {
                this.CommandId[n] = loopChecker.CommandId[n];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRunId(uint id)
        {
            if (this.RunIdCount >= this.RunId.Length)
            {
                Array.Resize(ref this.RunId, this.RunId.Length + InitialArray);
            }

            this.RunId[this.RunIdCount++] = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRunId()
        {
            if (this.RunIdCount == 0)
            {
                throw new InvalidOperationException();
            }

            this.RunIdCount--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommandId(uint id)
        {
            if (this.CommandIdCount >= this.CommandId.Length)
            {
                Array.Resize(ref this.CommandId, this.CommandId.Length + InitialArray);
            }

            this.CommandId[this.CommandIdCount++] = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCommandId()
        {
            if (this.CommandIdCount == 0)
            {
                throw new InvalidOperationException();
            }

            this.CommandIdCount--;
        }

        internal uint[] RunId;

        internal int RunIdCount;

        internal uint[] CommandId;

        internal int CommandIdCount;

        public LoopChecker Clone() => new(this);

        public override string ToString() => $"Run {this.RunIdCount}, Command {this.CommandIdCount}";
    }
#pragma warning restore SA1401 // Fields should be private

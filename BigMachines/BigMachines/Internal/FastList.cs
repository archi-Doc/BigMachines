// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BigMachines
{
    internal sealed class FastList<T> : IDisposable
    {
        private const int InitialCapacity = 4;

        private T?[] values = default!;
        private int count;

        public FastList()
        {
            this.Initialize();
        }

        public T?[] GetValues() => this.values; // no lock, safe for iterate

        public bool IsEmpty => this.count == 0;

        public int Add(T value)
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(FastList<T>));
            }

            if (this.freeIndex.Count != 0)
            {
                var index = this.freeIndex.Dequeue();
                this.values[index] = value;
                this.count++;
                return index;
            }
            else
            {// resize
                var newValues = new T[this.values.Length * 2];
                Array.Copy(this.values, 0, newValues, 0, this.values.Length);
                this.freeIndex.EnsureNewCapacity(newValues.Length);
                for (var i = this.values.Length; i < newValues.Length; i++)
                {
                    this.freeIndex.Enqueue(i);
                }

                var index = this.freeIndex.Dequeue();
                newValues[this.values.Length] = value;
                this.count++;
                Volatile.Write(ref this.values, newValues);
                return index;
            }
        }

        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.freeIndex = null!;
            this.values = Array.Empty<T?>();
            this.count = 0;
        }

        private void Initialize()
        {
            
        }
    }
}

// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BigMachines;

// NOT thread safe
public sealed class AddOnlyList<T> : IEnumerable<T>
{
    private const int InitialCapacity = 4;

    private T?[] values;
    private int count;

    public AddOnlyList()
    {
        this.count = 0;
        this.values = new T?[InitialCapacity];
    }

    public int Count => this.count;

    public int Add(T value)
    {
        if (this.count < this.values.Length)
        {
            this.values[this.count] = value;
            return this.count++;
        }
        else
        {// resize
            var newValues = new T[this.values.Length * 2];
            Array.Copy(this.values, 0, newValues, 0, this.values.Length);
            this.values = newValues;

            this.values[this.count] = value;
            return this.count++;
        }
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private AddOnlyList<T> list;
        private int index;
        private T? current;

        internal Enumerator(AddOnlyList<T> list)
        {
            this.list = list;
            this.index = 0;
            this.current = default(T);
        }

        public T Current => this.current!;

        object IEnumerator.Current
        {
            get
            {
                if (this.index == 0 || this.index == this.list.Count + 1)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.current!;
            }
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if ((uint)this.index < (uint)this.list.count)
            {
                this.current = this.list.values[this.index++];
                return true;
            }

            this.index = this.list.count + 1;
            this.current = default(T);
            return false;
        }

        public void Reset()
        {
            this.index = 0;
            this.current = default(T);
        }
    }
}

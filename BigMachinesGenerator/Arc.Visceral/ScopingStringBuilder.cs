// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Arc.Visceral
{
    /// <summary>
    /// Simple string builder with a scoping function, mainly created for a source generator.
    /// </summary>
    public class ScopingStringBuilder
    {
        public const int MaxIndentSpaces = 16;
        public const int MaxIndent = 32;

        public ScopingStringBuilder(int indentSpaces = 4)
        {
            this.CurrentScope = new Scope(this, false, false);

            // Indent spaces ranges from 0 to MaxIndentSpaces.
            this.IndentSpaces = indentSpaces < MaxIndentSpaces ? indentSpaces : MaxIndentSpaces;
            this.IndentSpaces = this.IndentSpaces > 0 ? this.IndentSpaces : 0;

            // Create indent string cache.
            this.IndentString = new string(' ', this.IndentSpaces);
        }

        /// <summary>
        /// Gets the number of indent spaces.
        /// </summary>
        public int IndentSpaces { get; }

        /// <summary>
        /// Gets the cached indent string.
        /// </summary>
        public string IndentString { get; }

        public IScope CurrentScope { get; private set; }

        public string CurrentObject => this.CurrentScope.CurrentObject;

        public string FullObject => this.CurrentScope.FullObject;

        public bool AddUsing(string @namespace)
        {
            if (@namespace == "System" || @namespace.StartsWith("System."))
            { // For sorting purpose.
                return this.usingSystem.Add(@namespace);
            }
            else
            { // Other namespaces.
                return this.usingOther.Add(@namespace);
            }
        }

        public void AddHeader(string header)
        {
            this.header.Add(header);
        }

        public IScope ScopeNamespace(string @namespace) => this.ScopeBrace($"namespace {@namespace}");

        public IScope ScopeBrace(string preface)
        {
            if (preface != null)
            {
                this.AppendLine(preface);
            }

            this.Append("{\r\n");
            return new Scope(this, true, true);
        }

        public IScope ScopeObject(string objectName, bool addPeriod = true) => new Scope(this, objectName, addPeriod);

        public IScope ScopeFullObject(string fullObjectName) => new Scope(this, fullObjectName);

        public void Append(string text, bool indentFlag = true)
        {
            if (this.CurrentScope.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Scope));
            }

            if (indentFlag)
            {
                var n = this.CurrentScope.CurrentIndent < MaxIndent ? this.CurrentScope.CurrentIndent : MaxIndent;
                while (n-- > 0)
                {
                    this.sb.Append(this.IndentString);
                }
            }

            this.sb.Append(text);
            return;
        }

        public void AppendLine(string? text = null, bool indentFlag = true)
        {
            if (this.CurrentScope.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Scope));
            }

            if (text != null)
            {
                this.Append(text, indentFlag);
            }

            this.Append("\r\n", false);
        }

        public void Append(ScopingStringBuilder ssb)
        {
            var list = ssb.sb.ToString().Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
            if (list == null)
            {
                return;
            }

            for (var i = 0; i < list.Length; i++)
            {
                if (i == (list.Length - 1) && list[i].Length == 0)
                {
                    break;
                }

                this.AppendLine(list[i]);
            }
        }

        public int IncrementIndent() => this.CurrentScope.IncrementIndent();

        public int DecrementIndent() => this.CurrentScope.DecrementIndent();

        /// <summary>
        /// Finalize and get the result. All scopes will be disposed.
        /// </summary>
        /// <returns>A result string.</returns>
        public string Finalize()
        {
            while (this.CurrentScope.Parent != null)
            {
                this.CurrentScope.Dispose();
            }

            var s = new StringBuilder();

            foreach (var x in this.header)
            {
                s.Append(x);
                s.Append("\r\n");
            }

            foreach (var x in this.usingSystem)
            {
                s.Append("using ");
                s.Append(x);
                s.Append(";\r\n");
            }

            foreach (var x in this.usingOther)
            {
                s.Append("using ");
                s.Append(x);
                s.Append(";\r\n");
            }

            if (this.header.Count > 0 || this.usingSystem.Count > 0 || this.usingOther.Count > 0)
            {
                s.Append("\r\n");
            }

            s.Append(this.sb);
            this.sb.Clear();
            this.header.Clear();
            this.usingSystem.Clear();
            this.usingOther.Clear();

            return s.ToString();
        }

        private StringBuilder sb = new StringBuilder();
        private List<string> header = new ();
        private SortedSet<string> usingSystem = new ();
        private SortedSet<string> usingOther = new ();

        public class Scope : IScope
        {
            public Scope(ScopingStringBuilder ssb, bool hasBrace, bool indentFlag)
            { // Brace scope
                this.ssb = ssb;
                this.Parent = this.ssb.CurrentScope;
                this.ssb.CurrentScope = this;

                this.HasBrace = hasBrace;
                if (this.Parent == null)
                {
                    this.CurrentIndent = 0;
                    this.FullObject = string.Empty;
                }
                else
                {
                    this.CurrentIndent = this.Parent.CurrentIndent;
                    this.FullObject = this.Parent.FullObject;
                }

                if (indentFlag)
                {
                    this.CurrentIndent++;
                }

                this.CurrentObject = string.Empty;
            }

            public Scope(ScopingStringBuilder ssb, string objectName, bool addPeriod)
            { // Object scope
                this.ssb = ssb;
                this.Parent = this.ssb.CurrentScope;
                this.ssb.CurrentScope = this;

                this.HasBrace = false;
                this.CurrentObject = objectName;
                if (this.Parent == null)
                {
                    this.CurrentIndent = 0;
                    this.FullObject = this.CurrentObject;
                }
                else
                {
                    this.CurrentIndent = this.Parent.CurrentIndent;
                    if (this.Parent.FullObject == string.Empty)
                    {
                        this.FullObject = objectName;
                    }
                    else
                    {
                        if (addPeriod)
                        {
                            this.FullObject = this.Parent.FullObject + "." + this.CurrentObject;
                        }
                        else
                        {
                            this.FullObject = this.Parent.FullObject + this.CurrentObject;
                        }
                    }
                }
            }

            public Scope(ScopingStringBuilder ssb, string fullObjectName)
            { // FullObject scope
                this.ssb = ssb;
                this.Parent = this.ssb.CurrentScope;
                this.ssb.CurrentScope = this;

                this.HasBrace = false;
                this.CurrentObject = fullObjectName;
                if (this.Parent == null)
                {
                    this.CurrentIndent = 0;
                    this.FullObject = this.CurrentObject;
                }
                else
                {
                    this.CurrentIndent = this.Parent.CurrentIndent;
                    this.FullObject = this.CurrentObject;
                }
            }

            public int IncrementIndent() => ++this.CurrentIndent;

            public int DecrementIndent() => this.CurrentIndent == 0 ? 0 : --this.CurrentIndent;

            public IScope? Parent { get; }

            public int CurrentIndent { get; private set; }

            public bool HasBrace { get; }

            public string CurrentObject { get; }

            public string FullObject { get; }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (this.IsDisposed || this.Parent == null)
                { // Already disposed or the root scope (the root scope cannot be disposed).
                    return;
                }

                if (this.ssb.CurrentScope != this)
                {
                    throw new InvalidOperationException("The disposal order of the scopes should be the reverse order in which they are created.");
                }

                this.ssb.CurrentScope = this.Parent;
                if (this.HasBrace)
                {
                    this.ssb.Append("}\r\n");
                }

                this.IsDisposed = true;
            }

            private ScopingStringBuilder ssb;
        }

        public interface IScope : IDisposable
        {
            bool IsDisposed { get; }

            IScope? Parent { get; }

            int CurrentIndent { get; }

            bool HasBrace { get; }

            string CurrentObject { get; }

            string FullObject { get; }

            int IncrementIndent();

            int DecrementIndent();
        }
    }
}

// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Arc.Visceral
{
    public enum VisceralGenericsKind
    {
        NotSet = 0,
        NotGeneric = 1,
        // UnboundGeneric = 2, // Currently not supported.
        OpenGeneric = 3,
        ClosedGeneric = 4,
    }

    /// <summary>
    /// Process generic syntax.
    /// </summary>
    public class VisceralGenerics
    {
        public VisceralGenerics()
        {
        }

        public void Add(GenericNameSyntax genericSyntax)
        {
            GenericsItem item;
            var identification = new GenericsIdentification(genericSyntax);

            if (!this.ItemDictionary.TryGetValue(identification, out item))
            {
                item = new GenericsItem(identification, genericSyntax);
                this.ItemDictionary.Add(identification, item);
            }
        }

        public void Prepare(Compilation compilation)
        {
            foreach (var x in this.ItemDictionary.Values)
            {
                var model = compilation.GetSemanticModel(x.GenericSyntax.SyntaxTree);
                var si = model.GetSymbolInfo(x.GenericSyntax);
                if (si.Symbol is INamedTypeSymbol ts)
                {
                    x.TypeSymbol = ts;
                    x.GenericsKind = VisceralHelper.TypeToGenericsKind(ts);
                }
            }
        }

        internal Dictionary<GenericsIdentification, GenericsItem> ItemDictionary { get; } = new();

        internal class GenericsItem
        {
            public GenericsIdentification Identification { get; }

            public GenericNameSyntax GenericSyntax { get; }

            public INamedTypeSymbol? TypeSymbol { get; set; }

            public VisceralGenericsKind GenericsKind { get; set; }

            public GenericsItem(GenericsIdentification identification, GenericNameSyntax genericSyntax)
            {
                this.Identification = identification;
                this.GenericSyntax = genericSyntax;
            }

            public override string ToString() => this.Identification.ToString();
        }

        internal readonly struct GenericsIdentification
        {// Generics Identification
            public GenericsIdentification(GenericNameSyntax genericSyntax)
            {
                this.TypeName = genericSyntax.Identifier.ValueText;

                var length = genericSyntax.TypeArgumentList.Arguments.Count;
                this.Arguments = new string[length];
                for (var i = 0; i < length; i++)
                {
                    this.Arguments[i] = genericSyntax.TypeArgumentList.Arguments[i].ToString();
                }
            }

            private readonly string TypeName;
            private readonly string[] Arguments;

            public override int GetHashCode()
            {// Consider HashCode.Combine();
                unchecked
                {
                    var hash = (17 * 31) + this.TypeName.GetHashCode();
                    foreach (var x in this.Arguments)
                    {
                        hash = (hash * 31) + x.GetHashCode();
                    }

                    return hash;
                }
            }

            public override bool Equals(object? obj)
            {
                if (obj == null || obj.GetType() != typeof(GenericsIdentification))
                {
                    return false;
                }

                var target = (GenericsIdentification)obj;
                if (this.TypeName != target.TypeName)
                {
                    return false;
                }
                else if (this.Arguments.Length != target.Arguments.Length)
                {
                    return false;
                }

                for (var i = 0; i < this.Arguments.Length; i++)
                {
                    if (this.Arguments[i] != target.Arguments[i])
                    {
                        return false;
                    }
                }

                // Identical
                return true;
            }

            public override string ToString()
            {
                var sb = new StringBuilder(this.TypeName);
                sb.Append('<');
                for (var i = 0; i < this.Arguments.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(this.Arguments[i]);
                }

                sb.Append('>');
                return sb.ToString();
            }
        }
    }
}

## ScopingStringBuilder

Simple string builder with a scoping function, mainly created for a source generator.

### Usage

```csharp
    var ssb = new ScopingStringBuilder(indentSpaces: 4);
    ssb.AddUsing("System"); // Add using directive.

    using var ns = ssb.ScopeNamespace("TestCode"); // Namespace.
    using (var cls = ssb.ScopeBrace("public class TestClass")) // Class declaration.
    {
        ssb.AppendLine("public string Text { get; set;}");
        ssb.AppendLine();

        using (var m = ssb.ScopeBrace("public void Hello()")) // Method.
        {
            var str = "world";
            ssb.AppendLine($"Console.Write(\"Hello {str}.\");");

            using var objThis = ssb.ScopeObject("this"); // Object scope.
            using var objText = ssb.ScopeObject("Text");
            ssb.AppendLine($"{objText.FullObject} = \"done.\";");
            ssb.AppendLine($"Console.WriteLine({ssb.FullObject});");
            ssb.AppendLine("return;");
        }
    }

    var result = ssb.Finalize(); // Finalize and get the result. All scopes will be disposed.
    Console.WriteLine(result);
```

The result:

```csharp
using System;

namespace TestCode
{
    public class TestClass
    {
        public string Text { get; set;}

        public void Hello()
        {
            Console.Write("Hello world.");
            this.Text = "done.";
            Console.WriteLine(this.Text);
            return;
        }
    }
}
```

### Scopes

#### Namespace

```csharp
ssb.ScopeNamespace("TestCode");
```

```
namespace TestCode
{
}
```

#### Brace

```csharp
ssb.ScopeNamespace("text");
```

```
text
{
}
```

#### Object

```csharp
using var a = ssb.ScopeObject("this"); // a.CurrentObject is "this", a.FullObject is "this".
using var b = ssb.ScopeObject("text");// b.CurrentObject is "text", b.FullObject is "this.text".
```


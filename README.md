## BigMachines is State Machine library for .NET
![Nuget](https://img.shields.io/nuget/v/BigMachines) ![Build and Test](https://github.com/archi-Doc/BigMachines/workflows/Build%20and%20Test/badge.svg)

- Very versatile and easy to use.

- Running machines and sending commands to each machine is designed to be lock-free.

- Full serialization features integrated with [Tinyhand](https://github.com/archi-Doc/Tinyhand).

- Simplify complex and long-running processes as much as possible.

  

Work in progress



## Table of Contents

- [Requirements](#requirements)



## Requirements

**C# 9.0** or later for generated codes.

**.NET 5** or later target framework.





## Machine Class

### Reserved keywords

These keywords in `Machine` class are reserved for source generator.

- `Interface`: Nested class for operating a machine.

- `State`: enum type which represents the state of a machine.
- `CreateInterface()`: Method for creating an interface.
- `RunInternal()`: 
- `ChangeStateInternal()`: 








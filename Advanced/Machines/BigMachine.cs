// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

// Create a BigMachine class that acts as the root for managing machines.
// In particular, define an empty partial class, add a BigMachineObject attribute, and then add AddMachine attributes for the Machine you want to include.
[BigMachineObject(Inclusive = true)]
[AddMachine<GenericMachine<string>>] // Add the generic machine manually as its type needs to be specified.
[AddMachine<ExternalMachine.Machine1>] // You can also reference external machines.
[AddMachine<ChildMachine>]
public partial class BigMachine;

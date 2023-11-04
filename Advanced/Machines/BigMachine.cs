// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[BigMachineObject(Inclusive = true)]
// [AddMachine<Advanced.TestMachine>]
[AddMachine<GenericMachine<string>>]
[AddMachine<ExternalMachine.Machine1>]
public partial class BigMachine;

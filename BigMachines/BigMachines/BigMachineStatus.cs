// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigMachines;

// [TinyhandObject]
public partial class BigMachineStatus
{
    // [Key(0)]
    public DateTime LastRun { get; internal protected set; }
}

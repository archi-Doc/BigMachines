// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

public static class ExternalMachineTest
{
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.Machine1.Get(); // Only one machine is created.
    }
}

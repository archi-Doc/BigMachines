// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Arc.Visceral;

public class VisceralSampleObject : VisceralObjectBase<VisceralSampleObject>
{
    public VisceralSampleObject()
    {
    }

    public new VisceralSampleBody Body => (VisceralSampleBody)((VisceralObjectBase<VisceralSampleObject>)this).Body;
}

public class VisceralSampleBody : VisceralBody<VisceralSampleObject>
{
    public VisceralSampleBody(GeneratorExecutionContext? context)
        : base(context)
    {
    }
}

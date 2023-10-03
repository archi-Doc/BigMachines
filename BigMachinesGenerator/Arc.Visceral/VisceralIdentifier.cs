// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;

#pragma warning disable SA1401 // Fields should be private

namespace Arc.Visceral;

public class VisceralIdentifier
{
    public static VisceralIdentifier Default = new VisceralIdentifier();
    // public static int IdentifierSerial = 0;

    private HashSet<string> identifier = new();
    private string identifierFormat = "__gen_visceral_identifier__";
    private int identifierSerial = 0;

    public VisceralIdentifier()
    {
    }

    public VisceralIdentifier(string identifierFormat)
    {
        this.identifierFormat = identifierFormat;
    }

    public bool Add(string identifier) => this.identifier.Add(identifier);

    public string GetIdentifier()
    {
        string identifier;
        while (true)
        {
            if (this.identifierSerial++ < 1_000)
            {// this.identifierSerial
                identifier = this.identifierFormat + this.identifierSerial.ToString("D3");
            }
            else
            {
                identifier = this.identifierFormat + this.identifierSerial.ToString();
            }

            if (this.identifier.Add(identifier))
            {
                return identifier;
            }
        }
    }
}

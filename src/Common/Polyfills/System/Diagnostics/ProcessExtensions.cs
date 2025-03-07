// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics;

internal static class ProcessExtensions
{
    public static void Kill(this Process process, bool entireProcessTree)
    {
        _ = entireProcessTree;
        process.Kill();
    }
}
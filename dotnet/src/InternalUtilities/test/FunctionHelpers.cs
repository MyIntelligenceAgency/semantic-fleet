#pragma warning disable IDE0073
// Copyright (c) Microsoft. All rights reserved.
#pragma warning restore IDE0073

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace SemanticKernel.UnitTests;

/// <summary>Test helpers for working with native functions.</summary>
internal static class FunctionHelpers
{
    /// <summary>
    /// Invokes a function on a skill instance via the kernel.
    /// </summary>
    public static Task<FunctionResult> CallViaKernel(
        object skillInstance,
        string methodName,
        params (string Name, string Value)[] variables)
    {
        var kernel = Kernel.Builder.Build();

        IDictionary<string, ISKFunction> importedFunctions = kernel.ImportFunctions(skillInstance);

        SKContext context = kernel.CreateNewContext();
        foreach ((string Name, string Value) pair in variables)
        {
            context.Variables.Set(pair.Name, pair.Value);
        }

        return importedFunctions[methodName].InvokeAsync(context);
    }
}

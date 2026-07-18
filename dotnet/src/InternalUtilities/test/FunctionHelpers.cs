#pragma warning disable IDE0073
// Copyright (c) Microsoft. All rights reserved.
#pragma warning restore IDE0073

using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace SemanticKernel.UnitTests;

/// <summary>Test helpers for working with native functions.</summary>
internal static class FunctionHelpers
{
    /// <summary>
    /// Invokes a function on a plugin instance via the kernel.
    /// </summary>
    public static async Task<FunctionResult> CallViaKernel(
        object skillInstance,
        string methodName,
        params (string Name, string Value)[] variables)
    {
        var kernel = Kernel.CreateBuilder().Build();

        KernelPlugin plugin = kernel.ImportPluginFromObject(skillInstance);

        var arguments = new KernelArguments();
        foreach ((string Name, string Value) pair in variables)
        {
            arguments[pair.Name] = pair.Value;
        }

        return await kernel.InvokeAsync(plugin[methodName], arguments).ConfigureAwait(false);
    }
}

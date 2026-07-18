#pragma warning disable IDE0073
// Copyright (c) Microsoft. All rights reserved.
#pragma warning restore IDE0073

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.SemanticKernel;

namespace SemanticKernel.IntegrationTests;

internal static class TestHelpers
{
    internal static void ImportSampleSkills(Kernel target)
    {
        GetSkills(target,
            "ChatSkill",
            "SummarizeSkill",
            "WriterSkill",
            "CalendarSkill",
            "ChildrensBookSkill",
            "ClassificationSkill",
            "CodingSkill",
            "FunSkill",
            "IntentDetectionSkill",
            "MiscSkill",
            "QASkill");
    }

    internal static IDictionary<string, KernelFunction> GetSkills(Kernel target, params string[] skillNames)
    {
        string? currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrWhiteSpace(currentAssemblyDirectory))
        {
            throw new InvalidOperationException("Unable to determine current assembly directory.");
        }

        string skillParentDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDirectory, "../../../../../../samples/skills"));

        // SK 1.78: ImportSemanticFunctionsFromDirectory(parentDir, names) was replaced by
        // Kernel.ImportPluginFromPromptDirectory(pluginDir, pluginName) which loads a single
        // plugin directory. We import each requested skill as its own plugin and flatten the
        // functions into the legacy name->function dictionary contract.
        var functions = new Dictionary<string, KernelFunction>(StringComparer.OrdinalIgnoreCase);
        foreach (var skillName in skillNames)
        {
            string pluginDirectory = Path.Combine(skillParentDirectory, skillName);
            if (!Directory.Exists(pluginDirectory))
            {
                continue;
            }

            KernelPlugin plugin = target.ImportPluginFromPromptDirectory(pluginDirectory, skillName);
            foreach (KernelFunction function in plugin)
            {
                functions[function.Name] = function;
            }
        }

        return functions;
    }
}

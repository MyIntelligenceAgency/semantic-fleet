// Copyright (c) MyIA. All rights reserved.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyIA.SemanticKernel.Connectors.AI.MultiConnector.ArithmeticMocks;

/// <summary>
/// Class representing an arithmetic engine capable of performing the 4 basic arithmetic operations, parsing and generating a prompt.
/// </summary>
public class ArithmeticEngine
{
    /// <summary>
    /// Function computing the result of an arithmetic operation on 2 operands. By default, it uses the Compute method, but can be changed to only support certain kinds of operations.
    /// </summary>
    public Func<ArithmeticOperation, int, int, int> ComputeFunc { get; set; } = Compute;

    /// <summary>
    /// Method computing the result of an arithmetic operation on 2 operands
    /// </summary>
    public static int Compute(ArithmeticOperation operation, int operand1, int operand2)
    {
        return operation switch
        {
            ArithmeticOperation.Add => operand1 + operand2,
            ArithmeticOperation.Subtract => operand1 - operand2,
            ArithmeticOperation.Multiply => operand1 * operand2,
            ArithmeticOperation.Divide => operand1 / operand2,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    /// <summary>
    /// Writes a prompt string for computing an arithmetic operation on 2 operands.
    /// </summary>
    public static string GeneratePrompt(ArithmeticOperation operation, int operand1, int operand2)
    {
        return $"Compute {operation}({operand1.ToString(CultureInfo.InvariantCulture)}, {operand2.ToString(CultureInfo.InvariantCulture)})";
    }

    /// <summary>
    /// Parses a prompt string for computing an arithmetic operation on 2 operands.
    /// </summary>
    public static (ArithmeticOperation operation, int operand1, int operand2) ParsePrompt(string prompt)
    {
        var match = Regex.Match(prompt, @"Compute (?<operation>.*)\((?<operand1>\d+), (?<operand2>\d+)\)");

        if (!match.Success)
        {
            throw new ArgumentException("Invalid prompt format.", nameof(prompt));
        }

        var parseSuccessful = Enum.TryParse<ArithmeticOperation>(match.Groups["operation"].Value, out var operation);
        var operand1 = int.Parse(match.Groups["operand1"].Value, CultureInfo.InvariantCulture);
        var operand2 = int.Parse(match.Groups["operand2"].Value, CultureInfo.InvariantCulture);

        return (operation, operand1, operand2);
    }

    /// <summary>
    /// Runs an arithmetic operation on 2 operands from a prompt string by parsing the prompt and then calling the ComputeFunc function.
    /// </summary>
    public string Run(string prompt)
    {
        var operation = ParsePrompt(prompt);
        return $"{this.ComputeFunc(operation.operation, operation.operand1, operation.operand2).ToString(CultureInfo.InvariantCulture)}";
    }
}

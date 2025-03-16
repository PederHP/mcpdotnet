using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;

namespace SemanticKernelIntegration.Plugins;

/// <summary>
/// Basic calculator plugin
/// </summary>
public class CalculatorPlugin
{
    /// <summary>
    /// Adds two numbers together
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>The sum of the two numbers</returns>
    [KernelFunction("add")]
    [Description("Add two numbers together")]
    public double Add(
        [Description("The first number to add")] double a,
        [Description("The second number to add")] double b)
    {
        Log.Debug("Add function called with a: {A}, b: {B}", a, b);
        var result = a + b;
        Log.Information("Addition result: {A} + {B} = {Result}", a, b, result);
        return result;
    }
    
    /// <summary>
    /// Multiplies two numbers
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>The product of the two numbers</returns>
    [KernelFunction("multiply")]
    [Description("Multiply two numbers together")]
    public double Multiply(
        [Description("The first number to multiply")] double a,
        [Description("The second number to multiply")] double b)
    {
        Log.Debug("Multiply function called with a: {A}, b: {B}", a, b);
        var result = a * b;
        Log.Information("Multiplication result: {A} * {B} = {Result}", a, b, result);
        return result;
    }
    
    /// <summary>
    /// Divides two numbers
    /// </summary>
    /// <param name="a">Dividend (number to be divided)</param>
    /// <param name="b">Divisor</param>
    /// <returns>The result of dividing a by b</returns>
    [KernelFunction("divide")]
    [Description("Divide the first number by the second number")]
    public string Divide(
        [Description("The dividend (number to be divided)")] double a,
        [Description("The divisor")] double b)
    {
        Log.Debug("Divide function called with a: {A}, b: {B}", a, b);
        
        if (b == 0)
        {
            Log.Warning("Division by zero attempted with a: {A}", a);
            return "Error: Cannot divide by zero";
        }
        
        var result = a / b;
        Log.Information("Division result: {A} / {B} = {Result}", a, b, result);
        return result.ToString();
    }
} 
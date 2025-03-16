using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Serilog;

namespace SemanticKernelIntegration.Plugins;

/// <summary>
/// Time-related plugin functions
/// </summary>
public class TimePlugin
{
    /// <summary>
    /// Gets the current UTC time
    /// </summary>
    /// <returns>The current UTC time formatted as RFC1123</returns>
    [KernelFunction]
    [Description("Gets the current time in UTC format")]
    public string GetCurrentUtcTime()
    {
        Log.Debug("GetCurrentUtcTime function called");
        var result = DateTime.UtcNow.ToString("R");
        Log.Information("Current UTC time: {UtcTime}", result);
        return result;
    }
    
    /// <summary>
    /// Gets the current local time
    /// </summary>
    /// <returns>The current local time as a string</returns>
    [KernelFunction]
    [Description("Gets the current local time")]
    public string GetLocalTime()
    {
        Log.Debug("GetLocalTime function called");
        var result = DateTime.Now.ToString("F");
        Log.Information("Current local time: {LocalTime}", result);
        return result;
    }
    
    /// <summary>
    /// Calculates the time difference between two dates
    /// </summary>
    /// <param name="startDate">The starting date in ISO format (e.g., 2023-01-01)</param>
    /// <param name="endDate">The ending date in ISO format (e.g., 2023-12-31)</param>
    /// <returns>The time difference in days</returns>
    [KernelFunction]
    [Description("Calculates time difference between two dates")]
    public string CalculateTimeDifference(
        [Description("Starting date in ISO format (e.g., 2023-01-01)")] string startDate,
        [Description("Ending date in ISO format (e.g., 2023-12-31)")] string endDate)
    {
        Log.Debug("CalculateTimeDifference function called with startDate: {StartDate}, endDate: {EndDate}", startDate, endDate);
        
        try
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);
            var difference = end - start;
            
            var result = $"The difference between {startDate} and {endDate} is {difference.Days} days.";
            Log.Information("Time difference calculated: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error calculating time difference: {ex.Message}";
            Log.Error(ex, "Failed to calculate time difference between {StartDate} and {EndDate}", startDate, endDate);
            return errorMessage;
        }
    }
} 
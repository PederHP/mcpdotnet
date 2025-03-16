using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;
using Serilog;

namespace SemanticKernelIntegration.Plugins;

/// <summary>
/// Weather information plugin
/// </summary>
public class WeatherPlugin
{
    // Simulated weather data
    private readonly Dictionary<string, (double Temperature, string Condition)> _cityWeather = new()
    {
        { "new york", (21.5, "Partly Cloudy") },
        { "london", (15.2, "Rainy") },
        { "tokyo", (28.0, "Sunny") },
        { "paris", (19.8, "Clear") },
        { "sydney", (25.5, "Sunny") },
        { "berlin", (17.3, "Cloudy") },
        { "moscow", (10.1, "Snowy") },
        { "dubai", (36.2, "Hot and Sunny") },
        { "rio de janeiro", (30.5, "Humid") },
        { "cape town", (22.7, "Windy") }
    };
    
    /// <summary>
    /// Gets the current weather for a specified city
    /// </summary>
    /// <param name="city">The name of the city</param>
    /// <returns>The current weather information for the city</returns>
    [KernelFunction("get_weather")]
    [Description("Get the current weather for a specified city")]
    public string GetWeather(
        [Description("The name of the city")] string city)
    {
        Log.Debug("GetWeather function called for city: {City}", city);
        var normalizedCity = city.Trim().ToLowerInvariant();
        
        if (_cityWeather.TryGetValue(normalizedCity, out var weather))
        {
            var cityName = char.ToUpper(normalizedCity[0]) + normalizedCity.Substring(1);
            var result = $"The current weather in {cityName} is {weather.Temperature}°C and {weather.Condition}.";
            Log.Information("Weather information retrieved for {City}: {Temperature}°C, {Condition}", 
                cityName, weather.Temperature, weather.Condition);
            return result;
        }
        
        Log.Warning("Weather data not found for city: {City}", city);
        return $"Sorry, I don't have weather data for {city}.";
    }
    
    /// <summary>
    /// Lists all cities for which weather data is available
    /// </summary>
    /// <returns>A comma-separated list of cities</returns>
    [KernelFunction("list_cities")]
    [Description("List all cities for which weather data is available")]
    public string ListAvailableCities()
    {
        Log.Debug("ListAvailableCities function called");
        
        var cities = _cityWeather.Keys
            .Select(city => char.ToUpper(city[0]) + city.Substring(1))
            .OrderBy(city => city);
        
        var cityList = string.Join(", ", cities);
        Log.Information("Available cities list generated with {Count} cities", cities.Count());
        return $"Available cities: {cityList}";
    }
    
    /// <summary>
    /// Gets a weather forecast for the next few days
    /// </summary>
    /// <param name="city">The name of the city</param>
    /// <param name="days">Number of days for the forecast (1-5)</param>
    /// <returns>A simulated weather forecast for the specified number of days</returns>
    [KernelFunction("get_forecast")]
    [Description("Get a weather forecast for a city for the specified number of days")]
    public string GetForecast(
        [Description("The name of the city")] string city,
        [Description("Number of days for the forecast (1-5)")] int days = 3)
    {
        Log.Debug("GetForecast function called for city: {City}, days: {Days}", city, days);
        
        var normalizedCity = city.Trim().ToLowerInvariant();
        
        if (!_cityWeather.TryGetValue(normalizedCity, out var currentWeather))
        {
            Log.Warning("Weather data not found for city: {City}", city);
            return $"Sorry, I don't have weather data for {city}.";
        }
        
        if (days < 1 || days > 5)
        {
            Log.Warning("Invalid number of days requested: {Days}. Must be between 1 and 5.", days);
            return "Please specify between 1 and 5 days for the forecast.";
        }
        
        // Generate a simple simulated forecast based on current conditions
        var random = new Random();
        var forecast = new List<string>();
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Clear", "Stormy" };
        
        var cityName = char.ToUpper(normalizedCity[0]) + normalizedCity.Substring(1);
        forecast.Add($"Weather forecast for {cityName}:");
        
        Log.Information("Generating {Days}-day forecast for {City}", days, cityName);
        
        for (int i = 0; i < days; i++)
        {
            var date = DateTime.Now.AddDays(i).ToString("dddd, MMMM d");
            var tempVariation = random.NextDouble() * 6 - 3; // -3 to +3 degrees variation
            var temp = Math.Round(currentWeather.Temperature + tempVariation, 1);
            var condition = conditions[random.Next(conditions.Length)];
            
            forecast.Add($"  {date}: {temp}°C, {condition}");
            
            Log.Debug("Forecast for {City} on {Date}: {Temperature}°C, {Condition}", 
                cityName, date, temp, condition);
        }
        
        var result = string.Join("\n", forecast);
        Log.Information("Forecast generated for {City} for {Days} days", cityName, days);
        
        return result;
    }
} 
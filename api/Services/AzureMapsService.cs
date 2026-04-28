// Disclaimer
// Notice: Any sample scripts, code, or commands comes with the following notification.
//
// This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production
// environment. THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute
// the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to
// market Your software product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your
// software product in which the Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our
// suppliers from and against any claims or lawsuits, including attorneys' fees, that arise or result from the use or
// distribution of the Sample Code.
//
// Please note: None of the conditions outlined in the disclaimer above will supersede the terms and conditions
// contained within the Customers Support Services Description.

using System.Net.Http.Json;
using System.Text.Json;
using GeoLoc.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeoLoc.Functions.Services;

public class AzureMapsService
{
    private const string BaseUrl = "https://atlas.microsoft.com/search/address/json";
    private readonly HttpClient _httpClient;
    private readonly string _subscriptionKey;
    private readonly ILogger<AzureMapsService> _logger;

    public AzureMapsService(HttpClient httpClient, IConfiguration configuration,
        ILogger<AzureMapsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _subscriptionKey = configuration["AzureMapsSubscriptionKey"]
            ?? throw new InvalidOperationException("AzureMapsSubscriptionKey is not configured.");
    }

    public async Task<GeoCoordinate?> GeocodeAddressAsync(string address)
    {
        _logger.LogInformation("Geocoding address: {Address}", address);

        var encodedAddress = Uri.EscapeDataString(address);
        var url = $"{BaseUrl}?api-version=1.0&subscription-key={_subscriptionKey}&query={encodedAddress}&countrySet=BR&language=pt-BR&limit=1";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var results = json.GetProperty("results");
        if (results.GetArrayLength() == 0)
        {
            _logger.LogWarning("No geocoding results found for address: {Address}", address);
            return null;
        }

        var position = results[0].GetProperty("position");
        var lat = position.GetProperty("lat").GetDouble();
        var lng = position.GetProperty("lon").GetDouble();

        _logger.LogInformation("Geocoded {Address} to ({Lat}, {Lng})", address, lat, lng);

        return new GeoCoordinate { Latitude = lat, Longitude = lng };
    }
}

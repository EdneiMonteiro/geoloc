// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

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

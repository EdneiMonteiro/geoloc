// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using GeoLoc.Functions.Models;
using GeoLoc.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GeoLoc.Functions.Functions;

public class ValidateLocationFunction
{
    private const double RadiusMeters = 50.0;

    private readonly TableStorageService _tableService;
    private readonly AzureMapsService _mapsService;
    private readonly GeoCalculationService _geoCalc;
    private readonly ILogger<ValidateLocationFunction> _logger;

    public ValidateLocationFunction(
        TableStorageService tableService,
        AzureMapsService mapsService,
        GeoCalculationService geoCalc,
        ILogger<ValidateLocationFunction> logger)
    {
        _tableService = tableService;
        _mapsService = mapsService;
        _geoCalc = geoCalc;
        _logger = logger;
    }

    [Function("validate-location")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // 1. Parse request body
        var requestBody = await req.ReadAsStringAsync();
        if (string.IsNullOrEmpty(requestBody))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                "Request body is required.");
        }

        var validationRequest = JsonSerializer.Deserialize<ValidationRequest>(requestBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (validationRequest is null || string.IsNullOrWhiteSpace(validationRequest.UserId))
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest,
                "userId is required.");
        }

        _logger.LogInformation(
            "Validating location for user {UserId}: ({Lat}, {Lng})",
            validationRequest.UserId, validationRequest.Latitude, validationRequest.Longitude);

        // 2. Look up user address in Table Storage
        var userAddress = await _tableService.GetUserAddressAsync(validationRequest.UserId);
        if (userAddress is null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.NotFound,
                $"No registered address found for user '{validationRequest.UserId}'.");
        }

        var searchableAddress = userAddress.ToSearchableAddress();

        // 3. Geocode the registered address via Azure Maps
        var addressCoords = await _mapsService.GeocodeAddressAsync(searchableAddress);
        if (addressCoords is null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.UnprocessableEntity,
                $"Could not geocode address: {searchableAddress}");
        }

        // 4. Calculate distance using Haversine formula
        var distanceMeters = _geoCalc.CalculateDistanceMeters(
            validationRequest.Latitude, validationRequest.Longitude,
            addressCoords.Latitude, addressCoords.Longitude);

        var isWithin = distanceMeters <= RadiusMeters;

        _logger.LogInformation(
            "User {UserId}: distance = {Distance:F2}m, within {Radius}m radius = {IsWithin}",
            validationRequest.UserId, distanceMeters, RadiusMeters, isWithin);

        // 5. Build and return result
        var result = new ValidationResult
        {
            IsWithinRadius = isWithin,
            DistanceMeters = Math.Round(distanceMeters, 2),
            RadiusMeters = RadiusMeters,
            RegisteredAddress = searchableAddress,
            DeviceCoordinates = new GeoCoordinate
            {
                Latitude = validationRequest.Latitude,
                Longitude = validationRequest.Longitude
            },
            AddressCoordinates = addressCoords
        };

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await response.Body.WriteAsync(
            System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, jsonOptions)));
        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }
}

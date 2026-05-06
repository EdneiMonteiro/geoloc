// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

namespace GeoLoc.Functions.Models;

public class ValidationRequest
{
    public string UserId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class GeoCoordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ValidationResult
{
    public bool IsWithinRadius { get; set; }
    public double DistanceMeters { get; set; }
    public double RadiusMeters { get; set; }
    public string RegisteredAddress { get; set; } = string.Empty;
    public GeoCoordinate DeviceCoordinates { get; set; } = new();
    public GeoCoordinate AddressCoordinates { get; set; } = new();
}

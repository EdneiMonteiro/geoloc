// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

namespace GeoLoc.Functions.Services;

public class GeoCalculationService
{
    private const double EarthRadiusMeters = 6_371_000;

    /// <summary>
    /// Calculates the distance in meters between two geographic coordinates
    /// using the Haversine formula.
    /// </summary>
    public double CalculateDistanceMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Returns true if the distance between the two coordinates is within the given radius.
    /// </summary>
    public bool IsWithinRadius(
        double lat1, double lon1,
        double lat2, double lon2,
        double radiusMeters)
    {
        return CalculateDistanceMeters(lat1, lon1, lat2, lon2) <= radiusMeters;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}

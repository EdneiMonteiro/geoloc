// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

using Azure.Data.Tables;

namespace GeoLoc.Functions.Models;

public class UserAddress : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string FullAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Builds a single-line address string suitable for geocoding.
    /// </summary>
    public string ToSearchableAddress()
        => $"{FullAddress}, {City}, {State}, {ZipCode}, {Country}";
}

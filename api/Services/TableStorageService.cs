// Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
// See LICENSE and DISCLAIMER.md in the project root for details.

using Azure.Data.Tables;
using GeoLoc.Functions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeoLoc.Functions.Services;

public class TableStorageService
{
    private const string TableName = "UserAddresses";
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageService> _logger;

    public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["TableStorageConnectionString"]
            ?? throw new InvalidOperationException("TableStorageConnectionString is not configured.");
        _tableClient = new TableClient(connectionString, TableName);
        _tableClient.CreateIfNotExists();
    }

    public async Task<UserAddress?> GetUserAddressAsync(string userId)
    {
        _logger.LogInformation("Looking up address for user {UserId}", userId);

        await foreach (var entity in _tableClient.QueryAsync<UserAddress>(
            filter: $"RowKey eq '{userId}'"))
        {
            _logger.LogInformation("Found address for user {UserId}: {Address}",
                userId, entity.ToSearchableAddress());
            return entity;
        }

        _logger.LogWarning("No address found for user {UserId}", userId);
        return null;
    }
}

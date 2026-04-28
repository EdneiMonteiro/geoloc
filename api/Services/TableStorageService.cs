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

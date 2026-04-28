#!/usr/bin/env bash
# Disclaimer
# Notice: Any sample scripts, code, or commands comes with the following notification.
#
# This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production
# environment. THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER
# EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
# PARTICULAR PURPOSE.
#
# We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute
# the object code form of the Sample Code, provided that You agree: (i) to not use Our name, logo, or trademarks to
# market Your software product in which the Sample Code is embedded; (ii) to include a valid copyright notice on Your
# software product in which the Sample Code is embedded; and (iii) to indemnify, hold harmless, and defend Us and Our
# suppliers from and against any claims or lawsuits, including attorneys' fees, that arise or result from the use or
# distribution of the Sample Code.
#
# Please note: None of the conditions outlined in the disclaimer above will supersede the terms and conditions
# contained within the Customers Support Services Description.

# seed-data.sh — Populates the UserAddresses table in Azure Table Storage with sample data.
#
# Usage:
#   For Azure Storage:      ./seed-data.sh <storage_account_name>
#   For local (Azurite):    ./seed-data.sh --local
#
# Prerequisites:
#   - Azure CLI installed and logged in (for Azure Storage)
#   - Azurite running locally on port 10002 (for --local)

set -euo pipefail

TABLE_NAME="UserAddresses"

if [[ "${1:-}" == "--local" ]]; then
    echo "==> Seeding local Azurite Table Storage..."
    CONNECTION_STRING="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
else
    STORAGE_ACCOUNT="${1:?Usage: $0 <storage_account_name> or $0 --local}"
    echo "==> Seeding Azure Table Storage in account: $STORAGE_ACCOUNT"
    CONNECTION_STRING=$(az storage account show-connection-string \
        --name "$STORAGE_ACCOUNT" \
        --query connectionString -o tsv)
fi

# Create table if it doesn't exist
az storage table create \
    --name "$TABLE_NAME" \
    --connection-string "$CONNECTION_STRING" \
    2>/dev/null || true

echo "==> Inserting sample user addresses..."

# User 1 — Av. Paulista, São Paulo
az storage entity insert \
    --table-name "$TABLE_NAME" \
    --connection-string "$CONNECTION_STRING" \
    --entity \
        PartitionKey=BR-SP \
        RowKey=user001 \
        FullAddress="Av. Paulista, 1000" \
        City="São Paulo" \
        State="SP" \
        ZipCode="01310-100" \
        Country="Brazil"

echo "    ✓ user001 — Av. Paulista, 1000, São Paulo"

# User 2 — Copacabana, Rio de Janeiro
az storage entity insert \
    --table-name "$TABLE_NAME" \
    --connection-string "$CONNECTION_STRING" \
    --entity \
        PartitionKey=BR-RJ \
        RowKey=user002 \
        FullAddress="Av. Atlântica, 2000" \
        City="Rio de Janeiro" \
        State="RJ" \
        ZipCode="22021-001" \
        Country="Brazil"

echo "    ✓ user002 — Av. Atlântica, 2000, Rio de Janeiro"

# User 3 — Praça da Liberdade, Belo Horizonte
az storage entity insert \
    --table-name "$TABLE_NAME" \
    --connection-string "$CONNECTION_STRING" \
    --entity \
        PartitionKey=BR-MG \
        RowKey=user003 \
        FullAddress="Praça da Liberdade, 1" \
        City="Belo Horizonte" \
        State="MG" \
        ZipCode="30140-010" \
        Country="Brazil"

echo "    ✓ user003 — Praça da Liberdade, 1, Belo Horizonte"

echo ""
echo "==> Seed complete! 3 users inserted into table '$TABLE_NAME'."
echo ""
echo "Available test users:"
echo "  user001 — Av. Paulista, 1000, São Paulo, SP"
echo "  user002 — Av. Atlântica, 2000, Rio de Janeiro, RJ"
echo "  user003 — Praça da Liberdade, 1, Belo Horizonte, MG"

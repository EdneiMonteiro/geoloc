#!/usr/bin/env bash
# Copyright (c) 2026 Ednei Monteiro. Licensed under the MIT License.
# See LICENSE and DISCLAIMER.md in the project root for details.

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

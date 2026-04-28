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

terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  tenant_id       = var.tenant_id
  subscription_id = var.subscription_id
}

# ---------- Resource Group ----------
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

# ---------- Storage Account (Table Storage for user addresses + Function App storage) ----------
resource "azurerm_storage_account" "main" {
  name                     = "${var.project_name}stor${random_string.suffix.result}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_table" "user_addresses" {
  name                 = "UserAddresses"
  storage_account_name = azurerm_storage_account.main.name
}

# ---------- Azure Maps Account ----------
resource "azurerm_maps_account" "main" {
  name                = "${var.project_name}-maps"
  resource_group_name = azurerm_resource_group.main.name
  location            = "global"
  sku_name            = "G2"
}

# ---------- Azure Functions (Consumption Plan, .NET 8 Isolated) ----------
resource "azurerm_service_plan" "main" {
  name                = "${var.project_name}-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "main" {
  name                       = "${var.project_name}-func-${random_string.suffix.result}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  service_plan_id            = azurerm_service_plan.main.id
  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }

    cors {
      allowed_origins = ["*"]
    }
  }

  app_settings = {
    "AzureWebJobsStorage"            = azurerm_storage_account.main.primary_connection_string
    "FUNCTIONS_WORKER_RUNTIME"       = "dotnet-isolated"
    "TableStorageConnectionString"   = azurerm_storage_account.main.primary_connection_string
    "AzureMapsSubscriptionKey"       = azurerm_maps_account.main.primary_access_key
    "WEBSITE_RUN_FROM_PACKAGE"       = "1"
  }
}

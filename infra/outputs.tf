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

output "resource_group_name" {
  value = azurerm_resource_group.main.name
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}

output "storage_connection_string" {
  value     = azurerm_storage_account.main.primary_connection_string
  sensitive = true
}

output "azure_maps_key" {
  value     = azurerm_maps_account.main.primary_access_key
  sensitive = true
}

output "function_app_name" {
  value = azurerm_linux_function_app.main.name
}

output "function_app_url" {
  value = "https://${azurerm_linux_function_app.main.default_hostname}"
}

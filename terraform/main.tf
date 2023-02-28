variable "prefix" {
  default = "aritest"
}

variable "db_sku" {
  default = "Basic"
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}
data "azurerm_subscription" "current" {}
data "azuread_application_published_app_ids" "well_known" {}

data "azuread_user" "user" {
  user_principal_name = "keyvault@arimitsu2023.onmicrosoft.com"
}

resource "azurerm_resource_group" "rg" {
  location = "japaneast"
  name     = "${var.prefix}-rg"
}

# AzureAD プリンシパル
resource "azuread_service_principal" "msgraph" {
  application_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph
  use_existing   = true
}

resource "azuread_application" "keyvault-app" {
  display_name     = "keyvault_app"
  sign_in_audience = "AzureADMyOrg"
  owners           = [data.azurerm_client_config.current.object_id]

  required_resource_access {
    # Microsoft Graph
    resource_app_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph

    resource_access {
      id   = azuread_service_principal.msgraph.app_role_ids["User.Read.All"]
      type = "Scope"
    }
  }

  public_client {
    redirect_uris = [
      "https://login.microsoftonline.com/common/oauth2/nativeclient",
      "http://localhost"
    ]
  }
}

resource "azurerm_key_vault" "keyvault" {
  name                        = "${var.prefix}-keyvault"
  location                    = azurerm_resource_group.rg.location
  resource_group_name         = azurerm_resource_group.rg.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false

  sku_name = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    key_permissions = [
      "Get",
    ]

    secret_permissions = [
      "Set",
      "Get",
      "Delete",
      "Purge",
      "Recover"
    ]

    storage_permissions = [
      "Get",
    ]
  }


  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azuread_user.user.object_id

    secret_permissions = [
      "Get",
      "List"
    ]
  }
}

resource "azurerm_key_vault_secret" "example" {
  name         = "secret-sauce"
  value        = "szechuan"
  key_vault_id = azurerm_key_vault.keyvault.id
}

output "keyvaultapp_application_id" {
  value = azuread_application.keyvault-app.application_id
}

output "keyvaultapp_object_id" {
  value = azuread_application.keyvault-app.object_id
}

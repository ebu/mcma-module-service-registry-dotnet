terraform {
  required_version = "~> 0.15.0"
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = "2.60.0"
    }
    azuread = {
      source = "hashicorp/azuread"
      version = "1.5.0"
    }
  }
}

provider "azurerm" {
  subscription_id = var.azure_subscription_id
  features {}
}

provider "azuread" {
  subscription_id = var.azure_subscription_id
  tenant_id = var.azure_tenant_id
}

locals {
  service_registry_api_zip_file      = "./../services/ServiceRegistry/Mcma.Azure.ServiceRegistry.ApiHandler/dist/function.zip"
  service_registry_url               = "https://${var.name}.azurewebsites.net"
  services_url                       = "${local.service_registry_url}/services"
  job_profiles_url                   = "${local.service_registry_url}/job-profiles"
  services_auth_type                 = "AzureAD"
  services_auth_context              = "{ \"scope\": \"${local.service_registry_url}/.default\" }"
}

data "azurerm_subscription" "primary" {}

data "azurerm_resource_group" "resource_group" {
  name = var.azure_resource_group_name
}

data "azurerm_app_service_plan" "app_service_plan" {
  resource_group_name = data.azurerm_resource_group.resource_group.name
  name = var.azure_app_service_plan_name
}

data "azurerm_cosmosdb_account" "cosmos_db_account" {
  resource_group_name = data.azurerm_resource_group.resource_group.name
  name = var.cosmosdb_account_name
}

data "azurerm_storage_account" "app_storage_account" {
  resource_group_name = data.azurerm_resource_group.resource_group.name
  name = var.app_storage_account_name
}

data "azurerm_storage_container" "app_storage_deploy_container" {
  resource_group_name = data.azurerm_resource_group.resource_group.name
  storage_account_name = data.azurerm_storage_account.app_storage_account.name
  name = var.app_storage_deploy_container
}

data "azurerm_application_insights" "appinsights" {
  resource_group_name = data.azurerm_resource_group.resource_group.name
  name = var.appinsights_name
}

data "azurerm_storage_account_sas" "app_storage_sas" {
  connection_string = data.azurerm_storage_account.app_storage_account.primary_connection_string
  https_only        = true

  resource_types {
    service   = false
    container = false
    object    = true
  }

  services {
    blob  = true
    queue = false
    table = false
    file  = false
  }

  start  = "2020-09-08"
  expiry = "2021-09-08"

  permissions {
    read    = true
    write   = false
    delete  = false
    list    = false
    add     = false
    create  = false
    update  = false
    process = false
  }
}

resource "azurerm_cosmosdb_sql_database" "cosmosdb_database" {
  name                = "ServiceRegistry"
  resource_group_name = data.azurerm_resource_group.resource_group.name
  account_name        = data.azurerm_cosmosdb_account.cosmos_db_account.name
}

resource "azurerm_cosmosdb_sql_container" "cosmosdb_container" {
  name                = "ServiceRegistry"
  resource_group_name = data.azurerm_resource_group.resource_group.name
  account_name        = data.azurerm_cosmosdb_account.cosmos_db_account.name
  database_name       = azurerm_cosmosdb_sql_database.cosmosdb_database.id
  partition_key_path  = "/partitionKey"
}

resource "azuread_application" "ad_app" {
  name            = var.name
  identifier_uris = [local.service_registry_url]
}

resource "azuread_service_principal" "service_principal" {
  application_id               = azuread_application.ad_app.application_id
  app_role_assignment_required = false
}

resource "azurerm_storage_blob" "api_function_zip" {
  name                   = "service-registry/function_${filesha256(local.service_registry_api_zip_file)}.zip"
  storage_account_name   = data.azurerm_storage_account.app_storage_account.name
  storage_container_name = data.azurerm_storage_container.app_storage_deploy_container.name
  type                   = "Block"
  source                 = local.service_registry_api_zip_file
}

resource "azurerm_function_app" "api_function" {
  name                       = var.name
  location                   = var.azure_location
  resource_group_name        = data.azurerm_resource_group.resource_group.name
  app_service_plan_id        = data.azurerm_app_service_plan.app_service_plan.id
  storage_account_name       = data.azurerm_storage_account.app_storage_account.name
  storage_account_access_key = data.azurerm_storage_account.app_storage_account.primary_access_key
  version                    = "~3"

  auth_settings {
    enabled                       = true
    issuer                        = "https://sts.windows.net/${var.azure_tenant_id}"
    default_provider              = "AzureActiveDirectory"
    unauthenticated_client_action = "RedirectToLoginPage"
    active_directory {
      client_id         = azuread_application.ad_app.application_id
      allowed_audiences = [local.service_registry_url]
    }
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME       = "dotnet"
    FUNCTION_APP_EDIT_MODE         = "readonly"
    https_only                     = true
    HASH                           = filesha256(local.service_registry_api_zip_file)
    WEBSITE_RUN_FROM_PACKAGE       = "${data.azurerm_storage_container.app_storage_deploy_container.id}/${azurerm_storage_blob.api_function_zip.name}${data.azurerm_storage_account_sas.app_storage_sas.sas}"
    APPINSIGHTS_INSTRUMENTATIONKEY = data.azurerm_application_insights.appinsights.instrumentation_key

    MCMA_TABLE_NAME            = azurerm_cosmosdb_sql_container.cosmosdb_container.name
    MCMA_PUBLIC_URL            = local.service_registry_url
    MCMA_COSMOS_DB_ENDPOINT    = data.azurerm_cosmosdb_account.cosmos_db_account.endpoint
    MCMA_COSMOS_DB_KEY         = data.azurerm_cosmosdb_account.cosmos_db_account.primary_master_key
    MCMA_COSMOS_DB_DATABASE_ID = var.cosmosdb_database_id
    MCMA_COSMOS_DB_REGION      = var.azure_location
  }
}

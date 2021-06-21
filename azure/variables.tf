variable "name" {
  type        = string
  description = "Name that serves as prefix for all managed resources in this module"
}

variable azure_subscription_id {
  type = string
  description = "Subscription under which all resources will be deployed"
}
variable azure_tenant_id {
  type = string
  description = "Azure AD tenant used for authenticating with Function Apps"
}
variable azure_location {
  type = string
  description = "Azure location in which to deploy all resources"
}
variable azure_resource_group_name {
  type = string
  description = "Resource group to which to deploy all resources"
}
variable azure_app_service_plan_name {
  type = string
  description = "App service plan to use for deployed Function Apps"
}

variable cosmosdb_account_name {
  type = string
  description = "Cosmos DB account in which to store data"
}
variable cosmosdb_database_id {
  type = string
  description = "Database in Cosmos DB in which to store data"
}

variable app_storage_account_name {
  type = string
  description = "Storage account to use for storing file related to running the application"
}
variable app_storage_deploy_container {
  type = string
  description = "Container in the storage account to use for storing the zip containing the application"
}

variable appinsights_name {
  type = string
  description = "Name of the ApplicationInsights instance to which to send logs"
}
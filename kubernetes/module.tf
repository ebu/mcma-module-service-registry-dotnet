terraform {
  required_providers {
    helm = {
      source = "hashicorp/helm"
      version = ">= 2.2.0"
    }
  }
}

provider "helm" {
  kubernetes {
    config_path = var.kubeconfig_path
  }
}

locals {
  values = [yamlencode({
    namespace               = var.kubernetes_namespace
    serviceName             = var.service_name
    mongoDb = {
      connectionString = var.mongodb_connection_string,
      databaseName = var.mongodb_database_name
      collectionName = var.mongodb_collection_name
    }
    apiHandler = {
      dockerImageId = var.api_handler_docker_image_id
      numReplicas   = var.api_handler_num_replicas
    }
  })]
}

resource "helm_release" "api_release" {
  name      = var.service_name
  chart     = "${path.module}/helm/service-registry"
  values    = local.values
  namespace = var.kubernetes_namespace
}
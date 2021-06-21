variable "service_name" {
    default = "service-registry"
}

variable "kubeconfig_path" {
    default = "~/.kube/config"
}
variable "kubernetes_namespace" {
    default = "default"
}

variable "mongodb_connection_string" {
    type = string
}
variable "mongodb_database_name" {
    default = "mcma"
}
variable "mongodb_collection_name" {
    default = "service-registry"
}

variable "api_handler_docker_image_id" {
    default = "evanverneyfink/mcma-service-registry-api"
}
variable "api_handler_num_replicas" {
    default = 1
}
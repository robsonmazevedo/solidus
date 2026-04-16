variable "resource_group_name" {
  description = "Nome do resource group do ambiente."
  type        = string
}

variable "location" {
  description = "Região do Azure para os recursos da plataforma."
  type        = string
}

variable "log_analytics_workspace_name" {
  description = "Nome do workspace do Log Analytics."
  type        = string
}

variable "container_app_environment_name" {
  description = "Nome do ambiente gerenciado do Azure Container Apps."
  type        = string
}

variable "log_analytics_sku" {
  description = "SKU do Log Analytics."
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_in_days" {
  description = "Retenção do Log Analytics em dias."
  type        = number
  default     = 30
}

variable "container_apps_public_network_access" {
  description = "Acesso público do Container Apps Environment."
  type        = string
  default     = "Enabled"
}

variable "tags" {
  description = "Tags padrão da plataforma."
  type        = map(string)
  default     = {}
}

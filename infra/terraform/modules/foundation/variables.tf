variable "resource_group_name" {
  description = "Nome do resource group do ambiente."
  type        = string
}

variable "location" {
  description = "Região do Azure para o ambiente."
  type        = string
}

variable "tags" {
  description = "Tags padrão do ambiente."
  type        = map(string)
  default     = {}
}

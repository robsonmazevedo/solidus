variable "project" {
  description = "Nome do projeto para tagging e convenções de nomenclatura."
  type        = string
  default     = "solidus"
}

variable "workload" {
  description = "Identificador curto da carga."
  type        = string
  default     = "solidus"
}

variable "environment" {
  description = "Ambiente alvo."
  type        = string
  default     = "dev"
}

variable "location" {
  description = "Região do Azure."
  type        = string
  default     = "East US 2"
}

variable "location_short" {
  description = "Abreviação curta da região para naming."
  type        = string
  default     = "eus2"
}

variable "owner" {
  description = "Responsável pelo ambiente."
  type        = string
  default     = "Solidus"
}

variable "cost_center" {
  description = "Centro de custo do projeto."
  type        = string
  default     = "TI"
}

variable "managed_by" {
  description = "Ferramenta responsável pela gestão do recurso."
  type        = string
  default     = "terraform"
}

variable "extra_tags" {
  description = "Tags adicionais opcionais."
  type        = map(string)
  default     = {}
}

variable "executor_public_ip" {
  description = "IP público do executor do Terraform. Se não informado, o bootstrap tenta descobri-lo automaticamente."
  type        = string
  default     = null
  nullable    = true
}

variable "additional_allowed_ip_rules" {
  description = "Lista adicional de IPs públicos ou ranges CIDR permitidos no firewall do Storage."
  type        = list(string)
  default     = []
}

variable "grant_current_executor_blob_access" {
  description = "Quando verdadeiro, atribui ao principal atual a role Storage Blob Data Contributor no container do estado."
  type        = bool
  default     = true
}

variable "project" {
  description = "Nome do projeto para tagging."
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

variable "log_analytics_sku" {
  description = "SKU do Log Analytics para o ambiente."
  type        = string
  default     = "PerGB2018"
}

variable "log_analytics_retention_in_days" {
  description = "Retenção do Log Analytics em dias."
  type        = number
  default     = 30
}

variable "container_apps_public_network_access" {
  description = "Acesso público do ambiente do Azure Container Apps."
  type        = string
  default     = "Enabled"
}

variable "postgres_admin_username" {
  description = "Usuário administrador padrão dos containers PostgreSQL."
  type        = string
  default     = "solidusadmin"
}

variable "postgres_registros_database_name" {
  description = "Nome do banco do domínio de registros."
  type        = string
  default     = "registros"
}

variable "postgres_posicao_database_name" {
  description = "Nome do banco do domínio de posição."
  type        = string
  default     = "posicao"
}

variable "rabbitmq_username" {
  description = "Usuário padrão do RabbitMQ."
  type        = string
  default     = "solidus"
}

variable "rabbitmq_image" {
  description = "Imagem do RabbitMQ usada no Container Apps."
  type        = string
  default     = "rabbitmq:4.2.5-management-alpine"
}

variable "rabbitmq_cpu" {
  description = "CPU reservada para o RabbitMQ."
  type        = number
  default     = 0.25
}

variable "rabbitmq_memory" {
  description = "Memória reservada para o RabbitMQ."
  type        = string
  default     = "0.5Gi"
}

variable "container_registry_sku" {
  description = "SKU do Azure Container Registry."
  type        = string
  default     = "Basic"
}

variable "enable_application_apps" {
  description = "Se true, cria também os Container Apps das aplicações de negócio."
  type        = bool
  default     = false
}

variable "app_environment" {
  description = "Ambiente lógico dos apps .NET no Azure."
  type        = string
  default     = "Development"
}

variable "jwt_issuer" {
  description = "Issuer usado pelos serviços HTTP."
  type        = string
  default     = "solidus"
}

variable "otlp_endpoint" {
  description = "Endpoint OTLP opcional para telemetria."
  type        = string
  default     = ""
}

variable "prometheus_endpoint" {
  description = "Endpoint Prometheus do worker."
  type        = string
  default     = "http://+:8082/"
}

variable "registros_ratelimit_permit" {
  description = "Permit limit da Registros API."
  type        = number
  default     = 100
}

variable "posicao_ratelimit_permit" {
  description = "Permit limit da Posição API."
  type        = number
  default     = 200
}

variable "registros_api_image" {
  description = "Imagem da Registros API no ACR."
  type        = string
  default     = ""
}

variable "posicao_api_image" {
  description = "Imagem da Posição API no ACR."
  type        = string
  default     = ""
}

variable "posicao_processor_image" {
  description = "Imagem do worker da Posição no ACR."
  type        = string
  default     = ""
}

variable "extra_tags" {
  description = "Tags adicionais opcionais."
  type        = map(string)
  default     = {}
}

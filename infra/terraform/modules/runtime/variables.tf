variable "resource_group_name" {
  description = "Nome do resource group do ambiente."
  type        = string
}

variable "location" {
  description = "Região do Azure para os recursos de runtime."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID do Azure Container Apps Environment."
  type        = string
}

variable "container_registry_name" {
  description = "Nome global do Azure Container Registry."
  type        = string
}

variable "container_registry_sku" {
  description = "SKU do Azure Container Registry."
  type        = string
  default     = "Basic"
}

variable "enable_application_apps" {
  description = "Controla se os apps de negócio devem ser criados agora ou apenas preparados para uma futura ativação."
  type        = bool
  default     = false
}

variable "app_environment" {
  description = "Valor de ASPNETCORE_ENVIRONMENT/DOTNET_ENVIRONMENT usado nos serviços."
  type        = string
  default     = "Development"
}

variable "jwt_issuer" {
  description = "Issuer configurado para os tokens JWT."
  type        = string
  default     = "solidus"
}

variable "otlp_endpoint" {
  description = "Endpoint OTLP opcional para telemetria."
  type        = string
  default     = ""
}

variable "prometheus_endpoint" {
  description = "Endpoint Prometheus usado pelo worker."
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

variable "registros_api_name" {
  description = "Nome do Container App da Registros API."
  type        = string
}

variable "posicao_api_name" {
  description = "Nome do Container App da Posição API."
  type        = string
}

variable "posicao_processor_name" {
  description = "Nome do Container App do worker de posição."
  type        = string
}

variable "registros_api_image" {
  description = "Imagem do ACR para a Registros API."
  type        = string
  default     = ""
}

variable "posicao_api_image" {
  description = "Imagem do ACR para a Posição API."
  type        = string
  default     = ""
}

variable "posicao_processor_image" {
  description = "Imagem do ACR para o worker de posição."
  type        = string
  default     = ""
}

variable "registros_connection_string" {
  description = "Connection string do banco de registros."
  type        = string
  sensitive   = true
}

variable "posicao_connection_string" {
  description = "Connection string do banco de posição."
  type        = string
  sensitive   = true
}

variable "redis_connection_string" {
  description = "Connection string do Redis."
  type        = string
  sensitive   = true
}

variable "rabbitmq_host" {
  description = "Host interno do RabbitMQ."
  type        = string
}

variable "rabbitmq_username" {
  description = "Usuário do RabbitMQ."
  type        = string
}

variable "rabbitmq_password" {
  description = "Senha do RabbitMQ."
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Tags padrão do ambiente."
  type        = map(string)
  default     = {}
}

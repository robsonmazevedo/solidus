variable "resource_group_name" {
  description = "Nome do resource group do ambiente."
  type        = string
}

variable "location" {
  description = "Região do Azure para os recursos de dados e mensageria."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID do Azure Container Apps Environment onde os serviços de dados serão hospedados."
  type        = string
}

variable "postgres_registros_server_name" {
  description = "Nome do Container App PostgreSQL do domínio de registros."
  type        = string
}

variable "postgres_posicao_server_name" {
  description = "Nome do Container App PostgreSQL do domínio de posição."
  type        = string
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

variable "postgres_admin_username" {
  description = "Usuário administrador dos containers PostgreSQL."
  type        = string
  default     = "solidusadmin"
}

variable "postgres_image" {
  description = "Imagem dos containers PostgreSQL."
  type        = string
  default     = "postgres:18.3-alpine"
}

variable "postgres_cpu" {
  description = "CPU reservada para cada PostgreSQL no Container Apps."
  type        = number
  default     = 0.25
}

variable "postgres_memory" {
  description = "Memória reservada para cada PostgreSQL no Container Apps."
  type        = string
  default     = "0.5Gi"
}

variable "redis_app_name" {
  description = "Nome do Container App do Redis."
  type        = string
}

variable "redis_image" {
  description = "Imagem do Redis usada no Container App."
  type        = string
  default     = "redis:8.6.2-alpine"
}

variable "redis_cpu" {
  description = "CPU reservada para o Redis no Container Apps."
  type        = number
  default     = 0.25
}

variable "redis_memory" {
  description = "Memória reservada para o Redis no Container Apps."
  type        = string
  default     = "0.5Gi"
}

variable "rabbitmq_app_name" {
  description = "Nome do Container App do RabbitMQ."
  type        = string
}

variable "rabbitmq_username" {
  description = "Usuário padrão do RabbitMQ."
  type        = string
  default     = "solidus"
}

variable "rabbitmq_image" {
  description = "Imagem do RabbitMQ usada no Container App."
  type        = string
  default     = "rabbitmq:4.2.5-management-alpine"
}

variable "rabbitmq_cpu" {
  description = "CPU reservada para o RabbitMQ no Container Apps."
  type        = number
  default     = 0.25
}

variable "rabbitmq_memory" {
  description = "Memória reservada para o RabbitMQ no Container Apps."
  type        = string
  default     = "0.5Gi"
}

variable "pgadmin_app_name" {
  description = "Nome do Container App do pgAdmin."
  type        = string
}

variable "pgadmin_image" {
  description = "Imagem do pgAdmin usada no Container App."
  type        = string
  default     = "dpage/pgadmin4:9.14.0"
}

variable "pgadmin_email" {
  description = "E-mail administrador padrão do pgAdmin."
  type        = string
  default     = "admin@solidus.dev"
}

variable "pgadmin_cpu" {
  description = "CPU reservada para o pgAdmin."
  type        = number
  default     = 0.25
}

variable "pgadmin_memory" {
  description = "Memória reservada para o pgAdmin."
  type        = string
  default     = "0.5Gi"
}

variable "redisinsight_app_name" {
  description = "Nome do Container App do RedisInsight."
  type        = string
}

variable "redisinsight_image" {
  description = "Imagem do RedisInsight usada no Container App."
  type        = string
  default     = "redis/redisinsight:3.2.0"
}

variable "redisinsight_cpu" {
  description = "CPU reservada para o RedisInsight."
  type        = number
  default     = 0.25
}

variable "redisinsight_memory" {
  description = "Memória reservada para o RedisInsight."
  type        = string
  default     = "0.5Gi"
}

variable "tags" {
  description = "Tags padrão do ambiente."
  type        = map(string)
  default     = {}
}

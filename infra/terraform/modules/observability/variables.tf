variable "resource_group_name" {
  description = "Nome do resource group do ambiente."
  type        = string
}

variable "location" {
  description = "Região do Azure para os recursos de observabilidade."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID do Azure Container Apps Environment."
  type        = string
}

variable "jaeger_app_name" {
  description = "Nome do Container App do Jaeger."
  type        = string
}

variable "prometheus_app_name" {
  description = "Nome do Container App do Prometheus."
  type        = string
}

variable "grafana_app_name" {
  description = "Nome do Container App do Grafana."
  type        = string
}

variable "redis_exporter_app_name" {
  description = "Nome do Container App do Redis Exporter."
  type        = string
}

variable "postgres_registros_exporter_app_name" {
  description = "Nome do Container App do exporter de métricas do PostgreSQL de registros."
  type        = string
}

variable "postgres_posicao_exporter_app_name" {
  description = "Nome do Container App do exporter de métricas do PostgreSQL de posição."
  type        = string
}

variable "registros_api_name" {
  description = "Nome esperado do Container App da Registros API."
  type        = string
}

variable "posicao_api_name" {
  description = "Nome esperado do Container App da Posição API."
  type        = string
}

variable "posicao_processor_name" {
  description = "Nome esperado do Container App do worker."
  type        = string
}

variable "rabbitmq_app_name" {
  description = "Nome do Container App do RabbitMQ."
  type        = string
}

variable "redis_hostname" {
  description = "Hostname interno do Redis containerizado."
  type        = string
}

variable "postgres_registros_hostname" {
  description = "Hostname interno do PostgreSQL de registros."
  type        = string
}

variable "postgres_posicao_hostname" {
  description = "Hostname interno do PostgreSQL de posição."
  type        = string
}

variable "postgres_admin_username" {
  description = "Usuário administrador padrão usado pelos exporters PostgreSQL."
  type        = string
}

variable "postgres_registros_database_name" {
  description = "Nome do banco de dados de registros monitorado pelo exporter."
  type        = string
}

variable "postgres_posicao_database_name" {
  description = "Nome do banco de dados de posição monitorado pelo exporter."
  type        = string
}

variable "postgres_registros_password" {
  description = "Senha do PostgreSQL de registros usada pelo exporter."
  type        = string
  sensitive   = true
}

variable "postgres_posicao_password" {
  description = "Senha do PostgreSQL de posição usada pelo exporter."
  type        = string
  sensitive   = true
}

variable "jaeger_image" {
  description = "Imagem do Jaeger all-in-one."
  type        = string
  default     = "jaegertracing/all-in-one:1.76.0"
}

variable "prometheus_image" {
  description = "Imagem do Prometheus."
  type        = string
  default     = "prom/prometheus:v3.11.2"
}

variable "grafana_image" {
  description = "Imagem do Grafana."
  type        = string
  default     = "grafana/grafana:13.0.0"
}

variable "redis_exporter_image" {
  description = "Imagem do Redis Exporter."
  type        = string
  default     = "oliver006/redis_exporter:v1.82.0"
}

variable "postgres_exporter_image" {
  description = "Imagem oficial do PostgreSQL Exporter."
  type        = string
  default     = "quay.io/prometheuscommunity/postgres-exporter:v0.19.1"
}

variable "prometheus_retention" {
  description = "Retenção do TSDB do Prometheus."
  type        = string
  default     = "7d"
}

variable "grafana_admin_username" {
  description = "Usuário administrador do Grafana."
  type        = string
  default     = "admin"
}

variable "tags" {
  description = "Tags padrão do ambiente."
  type        = map(string)
  default     = {}
}

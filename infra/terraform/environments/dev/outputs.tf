output "resource_group_name" {
  description = "Nome do resource group base do ambiente dev."
  value       = module.foundation.resource_group_name
}

output "log_analytics_workspace_name" {
  description = "Nome do Log Analytics Workspace do ambiente dev."
  value       = module.platform.log_analytics_workspace_name
}

output "container_app_environment_name" {
  description = "Nome do Container Apps Environment do ambiente dev."
  value       = module.platform.container_app_environment_name
}

output "container_app_environment_default_domain" {
  description = "Domínio padrão do Container Apps Environment do ambiente dev."
  value       = module.platform.container_app_environment_default_domain
}

output "postgres_registros_server_fqdn" {
  description = "FQDN do PostgreSQL do domínio de registros."
  value       = module.data.postgres_registros_server_fqdn
}

output "postgres_posicao_server_fqdn" {
  description = "FQDN do PostgreSQL do domínio de posição."
  value       = module.data.postgres_posicao_server_fqdn
}

output "redis_hostname" {
  description = "Hostname do Redis."
  value       = module.data.redis_hostname
}

output "rabbitmq_host" {
  description = "Nome interno do RabbitMQ para uso pelos serviços no Container Apps."
  value       = module.data.rabbitmq_host
}

output "rabbitmq_management_url" {
  description = "URL pública da interface do RabbitMQ."
  value       = module.data.rabbitmq_management_url
}

output "rabbitmq_username" {
  description = "Usuário do RabbitMQ."
  value       = module.data.rabbitmq_username
}

output "registros_connection_string" {
  description = "Connection string do PostgreSQL de registros."
  value       = module.data.registros_connection_string
  sensitive   = true
}

output "posicao_connection_string" {
  description = "Connection string do PostgreSQL de posição."
  value       = module.data.posicao_connection_string
  sensitive   = true
}

output "redis_connection_string" {
  description = "Connection string do Redis para StackExchange.Redis."
  value       = module.data.redis_connection_string
  sensitive   = true
}

output "rabbitmq_password" {
  description = "Senha do RabbitMQ."
  value       = module.data.rabbitmq_password
  sensitive   = true
}

output "pgadmin_url" {
  description = "URL pública do pgAdmin."
  value       = module.data.pgadmin_url
}

output "pgadmin_email" {
  description = "E-mail admin do pgAdmin."
  value       = module.data.pgadmin_email
}

output "pgadmin_password" {
  description = "Senha admin do pgAdmin."
  value       = module.data.pgadmin_password
  sensitive   = true
}

output "redisinsight_url" {
  description = "URL pública do RedisInsight."
  value       = module.data.redisinsight_url
}

output "container_registry_name" {
  description = "Nome do Azure Container Registry."
  value       = module.runtime.container_registry_name
}

output "container_registry_login_server" {
  description = "Login server do Azure Container Registry."
  value       = module.runtime.container_registry_login_server
}

output "container_registry_admin_username" {
  description = "Usuário admin do ACR."
  value       = module.runtime.container_registry_admin_username
}

output "container_registry_admin_password" {
  description = "Senha admin do ACR."
  value       = module.runtime.container_registry_admin_password
  sensitive   = true
}

output "jwt_secret" {
  description = "Secret JWT compartilhado pelos serviços HTTP."
  value       = module.runtime.jwt_secret
  sensitive   = true
}

output "registros_api_url" {
  description = "URL pública da Registros API quando os apps estiverem habilitados."
  value       = module.runtime.registros_api_url
}

output "posicao_api_url" {
  description = "URL pública da Posição API quando os apps estiverem habilitados."
  value       = module.runtime.posicao_api_url
}

output "jaeger_url" {
  description = "URL do Jaeger UI."
  value       = module.observability.jaeger_url
}

output "prometheus_url" {
  description = "URL do Prometheus."
  value       = module.observability.prometheus_url
}

output "grafana_url" {
  description = "URL do Grafana."
  value       = module.observability.grafana_url
}

output "grafana_admin_username" {
  description = "Usuário admin do Grafana."
  value       = module.observability.grafana_admin_username
}

output "grafana_admin_password" {
  description = "Senha admin do Grafana."
  value       = module.observability.grafana_admin_password
  sensitive   = true
}

output "postgres_registros_server_name" {
  description = "Nome do PostgreSQL do domínio de registros."
  value       = azurerm_container_app.postgres_registros.name
}

output "postgres_registros_server_fqdn" {
  description = "Host interno do PostgreSQL do domínio de registros."
  value       = azurerm_container_app.postgres_registros.ingress[0].fqdn
}

output "postgres_posicao_server_name" {
  description = "Nome do PostgreSQL do domínio de posição."
  value       = azurerm_container_app.postgres_posicao.name
}

output "postgres_posicao_server_fqdn" {
  description = "Host interno do PostgreSQL do domínio de posição."
  value       = azurerm_container_app.postgres_posicao.ingress[0].fqdn
}

output "postgres_registros_password" {
  description = "Senha do PostgreSQL do domínio de registros."
  value       = random_password.postgres_registros.result
  sensitive   = true
}

output "postgres_posicao_password" {
  description = "Senha do PostgreSQL do domínio de posição."
  value       = random_password.postgres_posicao.result
  sensitive   = true
}

output "redis_hostname" {
  description = "Hostname interno do Redis containerizado."
  value       = azurerm_container_app.redis.name
}

output "rabbitmq_host" {
  description = "Nome interno do app RabbitMQ para uso pelos serviços no Container Apps."
  value       = azurerm_container_app.rabbitmq.name
}

output "rabbitmq_management_url" {
  description = "URL pública da interface de administração do RabbitMQ."
  value       = "https://${azurerm_container_app.rabbitmq.ingress[0].fqdn}"
}

output "rabbitmq_username" {
  description = "Usuário do RabbitMQ."
  value       = var.rabbitmq_username
}

output "rabbitmq_password" {
  description = "Senha do RabbitMQ."
  value       = random_password.rabbitmq.result
  sensitive   = true
}

output "pgadmin_url" {
  description = "URL pública do pgAdmin."
  value       = "https://${azurerm_container_app.pgadmin.latest_revision_fqdn}"
}

output "pgadmin_email" {
  description = "E-mail admin do pgAdmin."
  value       = var.pgadmin_email
}

output "pgadmin_password" {
  description = "Senha admin do pgAdmin."
  value       = random_password.pgadmin.result
  sensitive   = true
}

output "redisinsight_url" {
  description = "URL pública do RedisInsight."
  value       = "https://${azurerm_container_app.redisinsight.latest_revision_fqdn}"
}

output "registros_connection_string" {
  description = "Connection string do PostgreSQL de registros."
  value       = "Host=${azurerm_container_app.postgres_registros.ingress[0].fqdn};Port=5432;Database=${var.postgres_registros_database_name};Username=${var.postgres_admin_username};Password=${random_password.postgres_registros.result}"
  sensitive   = true
}

output "posicao_connection_string" {
  description = "Connection string do PostgreSQL de posição."
  value       = "Host=${azurerm_container_app.postgres_posicao.ingress[0].fqdn};Port=5432;Database=${var.postgres_posicao_database_name};Username=${var.postgres_admin_username};Password=${random_password.postgres_posicao.result}"
  sensitive   = true
}

output "redis_connection_string" {
  description = "Connection string do Redis containerizado para StackExchange.Redis."
  value       = "${azurerm_container_app.redis.name}:6379,abortConnect=False"
  sensitive   = true
}


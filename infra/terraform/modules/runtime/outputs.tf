output "container_registry_name" {
  description = "Nome do Azure Container Registry."
  value       = data.azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "Login server do Azure Container Registry."
  value       = data.azurerm_container_registry.main.login_server
}

output "container_registry_admin_username" {
  description = "Usuário admin do ACR para cenários iniciais de build/push."
  value       = data.azurerm_container_registry.main.admin_username
}

output "container_registry_admin_password" {
  description = "Senha admin do ACR para cenários iniciais de build/push."
  value       = data.azurerm_container_registry.main.admin_password
  sensitive   = true
}

output "jwt_secret" {
  description = "Secret JWT compartilhado pelos serviços HTTP."
  value       = random_password.jwt_secret.result
  sensitive   = true
}

output "registros_api_url" {
  description = "URL pública da Registros API quando habilitada."
  value       = try(azurerm_container_app.registros_api[0].latest_revision_fqdn, null)
}

output "posicao_api_url" {
  description = "URL pública da Posição API quando habilitada."
  value       = try(azurerm_container_app.posicao_api[0].latest_revision_fqdn, null)
}

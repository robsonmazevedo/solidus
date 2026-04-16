output "log_analytics_workspace_id" {
  description = "ID do Log Analytics Workspace."
  value       = azurerm_log_analytics_workspace.main.id
}

output "log_analytics_workspace_name" {
  description = "Nome do Log Analytics Workspace."
  value       = azurerm_log_analytics_workspace.main.name
}

output "container_app_environment_id" {
  description = "ID do Container Apps Environment."
  value       = azurerm_container_app_environment.main.id
}

output "container_app_environment_name" {
  description = "Nome do Container Apps Environment."
  value       = azurerm_container_app_environment.main.name
}

output "container_app_environment_default_domain" {
  description = "Domínio padrão gerado para o Container Apps Environment."
  value       = azurerm_container_app_environment.main.default_domain
}

output "jaeger_url" {
  description = "URL do Jaeger UI."
  value       = "https://${azurerm_container_app.jaeger.latest_revision_fqdn}"
}

output "prometheus_url" {
  description = "URL do Prometheus."
  value       = "https://${azurerm_container_app.prometheus.latest_revision_fqdn}"
}

output "grafana_url" {
  description = "URL do Grafana."
  value       = "https://${azurerm_container_app.grafana.latest_revision_fqdn}"
}

output "grafana_admin_username" {
  description = "Usuário admin do Grafana."
  value       = var.grafana_admin_username
}

output "grafana_admin_password" {
  description = "Senha admin do Grafana."
  value       = random_password.grafana_admin_password.result
  sensitive   = true
}

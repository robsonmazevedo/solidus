output "state_resource_group_name" {
  description = "Nome do resource group do backend remoto."
  value       = azurerm_resource_group.tfstate.name
}

output "state_storage_account_name" {
  description = "Nome da storage account do backend remoto."
  value       = azurerm_storage_account.tfstate.name
}

output "state_container_name" {
  description = "Nome do container do backend remoto."
  value       = azurerm_storage_container.tfstate.name
}

output "state_key" {
  description = "Chave inicial sugerida para o ambiente dev."
  value       = "solidus-dev.tfstate"
}

output "executor_public_ip" {
  description = "IP público identificado para o executor atual do Terraform."
  value       = local.executor_public_ip
}

output "allowed_storage_ip_rules" {
  description = "Regras de IP efetivamente permitidas no firewall do Storage."
  value       = local.allowed_storage_ip_rules
}

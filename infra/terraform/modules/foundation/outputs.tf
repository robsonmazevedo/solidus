output "resource_group_id" {
  description = "ID do resource group provisionado."
  value       = azurerm_resource_group.main.id
}

output "resource_group_name" {
  description = "Nome do resource group provisionado."
  value       = azurerm_resource_group.main.name
}

data "azurerm_client_config" "current" {}

data "http" "current_public_ip" {
  count = var.executor_public_ip == null ? 1 : 0
  url   = "https://api.ipify.org?format=json"

  request_headers = {
    Accept = "application/json"
  }

  retry {
    attempts     = 2
    min_delay_ms = 500
    max_delay_ms = 2000
  }
}

locals {
  state_resource_group_name = "rg-${var.workload}-tfstate-${var.environment}"

  base_tags = merge({
    project     = var.project
    workload    = var.workload
    environment = var.environment
    owner       = var.owner
    cost_center = var.cost_center
    managed_by  = var.managed_by
    phase       = "foundation"
  }, var.extra_tags)

  executor_public_ip = var.executor_public_ip != null ? var.executor_public_ip : try(jsondecode(data.http.current_public_ip[0].response_body).ip, null)

  allowed_storage_ip_rules = distinct(compact(concat(
    local.executor_public_ip == null ? [] : [local.executor_public_ip],
    var.additional_allowed_ip_rules
  )))
}

resource "azurerm_resource_group" "tfstate" {
  name     = local.state_resource_group_name
  location = var.location
  tags     = local.base_tags
}

resource "azurerm_storage_account" "tfstate" {
  name                            = substr(lower("st${var.workload}${var.environment}${var.location_short}tf"), 0, 24)
  resource_group_name             = azurerm_resource_group.tfstate.name
  location                        = azurerm_resource_group.tfstate.location
  account_kind                    = "StorageV2"
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  public_network_access_enabled   = true
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false
  default_to_oauth_authentication = true

  network_rules {
    default_action = "Deny"
    bypass         = ["AzureServices"]
    ip_rules       = local.allowed_storage_ip_rules
  }

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 7
    }

    container_delete_retention_policy {
      days = 7
    }
  }

  tags = local.base_tags

  lifecycle {
    precondition {
      condition     = length(local.allowed_storage_ip_rules) > 0
      error_message = "Nenhum IP público permitido foi resolvido para o firewall do Storage. Informe executor_public_ip ou additional_allowed_ip_rules."
    }
  }
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}

resource "azurerm_role_assignment" "current_executor_blob_access" {
  count = var.grant_current_executor_blob_access ? 1 : 0

  scope                = azurerm_storage_container.tfstate.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
  description          = "Permite ao executor atual do Terraform ler e escrever os blobs do estado remoto."
}

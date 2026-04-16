locals {
  resource_group_name                  = "rg-${var.workload}-${var.environment}-${var.location_short}"
  log_analytics_workspace_name         = "log-${var.workload}-${var.environment}-${var.location_short}"
  container_app_environment_name       = "cae-${var.workload}-${var.environment}-${var.location_short}"
  postgres_registros_server_name       = "psql-${var.workload}-${var.environment}-${var.location_short}-reg"
  postgres_posicao_server_name         = "psql-${var.workload}-${var.environment}-${var.location_short}-pos"
  redis_app_name                       = "redis-${var.workload}-${var.environment}-${var.location_short}"
  rabbitmq_app_name                    = "rabbitmq-${var.workload}-${var.environment}-${var.location_short}"
  pgadmin_app_name                     = "pgadmin-${var.workload}-${var.environment}-${var.location_short}"
  redisinsight_app_name                = "redisinsight-${var.workload}-${var.environment}-${var.location_short}"
  state_resource_group_name            = "rg-${var.workload}-${var.environment}-${var.location_short}-tfstate"
  container_registry_name              = "acr${var.workload}${var.environment}${var.location_short}"
  registros_api_name                   = "registros-api-${var.workload}-${var.environment}-${var.location_short}"
  posicao_api_name                     = "posicao-api-${var.workload}-${var.environment}-${var.location_short}"
  posicao_processor_name               = "posicao-processor-${var.workload}-${var.environment}-${var.location_short}"
  jaeger_app_name                      = "jaeger-${var.workload}-${var.environment}-${var.location_short}"
  prometheus_app_name                  = "prometheus-${var.workload}-${var.environment}-${var.location_short}"
  grafana_app_name                     = "grafana-${var.workload}-${var.environment}-${var.location_short}"
  redis_exporter_app_name              = "redis-exporter-${var.workload}-${var.environment}-${var.location_short}"
  postgres_registros_exporter_app_name = "pgexp-${var.workload}-${var.environment}-${var.location_short}-reg"
  postgres_posicao_exporter_app_name   = "pgexp-${var.workload}-${var.environment}-${var.location_short}-pos"

  tags = merge({
    project     = var.project
    workload    = var.workload
    environment = var.environment
    owner       = var.owner
    cost_center = var.cost_center
    managed_by  = var.managed_by
    phase       = "platform"
  }, var.extra_tags)
}

module "foundation" {
  source = "../../modules/foundation"

  resource_group_name = local.resource_group_name
  location            = var.location
  tags                = local.tags
}

module "platform" {
  source = "../../modules/platform"

  resource_group_name                  = module.foundation.resource_group_name
  location                             = var.location
  log_analytics_workspace_name         = local.log_analytics_workspace_name
  container_app_environment_name       = local.container_app_environment_name
  log_analytics_sku                    = var.log_analytics_sku
  log_analytics_retention_in_days      = var.log_analytics_retention_in_days
  container_apps_public_network_access = var.container_apps_public_network_access
  tags                                 = local.tags
}

module "data" {
  source = "../../modules/data"

  resource_group_name              = module.foundation.resource_group_name
  location                         = var.location
  container_app_environment_id     = module.platform.container_app_environment_id
  postgres_registros_server_name   = local.postgres_registros_server_name
  postgres_posicao_server_name     = local.postgres_posicao_server_name
  postgres_registros_database_name = var.postgres_registros_database_name
  postgres_posicao_database_name   = var.postgres_posicao_database_name
  postgres_admin_username          = var.postgres_admin_username
  redis_app_name                   = local.redis_app_name
  rabbitmq_app_name                = local.rabbitmq_app_name
  rabbitmq_username                = var.rabbitmq_username
  rabbitmq_image                   = var.rabbitmq_image
  pgadmin_app_name                 = local.pgadmin_app_name
  redisinsight_app_name            = local.redisinsight_app_name
  rabbitmq_cpu                     = var.rabbitmq_cpu
  rabbitmq_memory                  = var.rabbitmq_memory
  tags                             = local.tags
}

module "runtime" {
  source = "../../modules/runtime"

  resource_group_name          = module.foundation.resource_group_name
  location                     = var.location
  container_app_environment_id = module.platform.container_app_environment_id
  container_registry_name                = local.container_registry_name
  container_registry_resource_group_name = local.state_resource_group_name
  container_registry_sku                 = var.container_registry_sku
  enable_application_apps      = var.enable_application_apps
  app_environment              = var.app_environment
  jwt_issuer                   = var.jwt_issuer
  otlp_endpoint                = var.otlp_endpoint
  prometheus_endpoint          = var.prometheus_endpoint
  registros_ratelimit_permit   = var.registros_ratelimit_permit
  posicao_ratelimit_permit     = var.posicao_ratelimit_permit
  registros_api_name           = local.registros_api_name
  posicao_api_name             = local.posicao_api_name
  posicao_processor_name       = local.posicao_processor_name
  registros_api_image          = var.registros_api_image
  posicao_api_image            = var.posicao_api_image
  posicao_processor_image      = var.posicao_processor_image
  registros_connection_string  = module.data.registros_connection_string
  posicao_connection_string    = module.data.posicao_connection_string
  redis_connection_string      = module.data.redis_connection_string
  rabbitmq_host                = module.data.rabbitmq_host
  rabbitmq_username            = module.data.rabbitmq_username
  rabbitmq_password            = module.data.rabbitmq_password
  tags                         = local.tags
}

module "observability" {
  source = "../../modules/observability"

  resource_group_name                  = module.foundation.resource_group_name
  location                             = var.location
  container_app_environment_id         = module.platform.container_app_environment_id
  jaeger_app_name                      = local.jaeger_app_name
  prometheus_app_name                  = local.prometheus_app_name
  grafana_app_name                     = local.grafana_app_name
  redis_exporter_app_name              = local.redis_exporter_app_name
  postgres_registros_exporter_app_name = local.postgres_registros_exporter_app_name
  postgres_posicao_exporter_app_name   = local.postgres_posicao_exporter_app_name
  registros_api_name                   = local.registros_api_name
  posicao_api_name                     = local.posicao_api_name
  posicao_processor_name               = local.posicao_processor_name
  rabbitmq_app_name                    = local.rabbitmq_app_name
  redis_hostname                       = module.data.redis_hostname
  postgres_registros_hostname          = module.data.postgres_registros_server_name
  postgres_posicao_hostname            = module.data.postgres_posicao_server_name
  postgres_admin_username              = var.postgres_admin_username
  postgres_registros_database_name     = var.postgres_registros_database_name
  postgres_posicao_database_name       = var.postgres_posicao_database_name
  postgres_registros_password          = module.data.postgres_registros_password
  postgres_posicao_password            = module.data.postgres_posicao_password
  tags                                 = local.tags
}

terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
    }
    azapi = {
      source = "azure/azapi"
    }
    random = {
      source = "hashicorp/random"
    }
  }
}

resource "random_password" "postgres_registros" {
  length           = 24
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "postgres_posicao" {
  length           = 24
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "rabbitmq" {
  length           = 24
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "random_password" "pgadmin" {
  length  = 20
  special = false
}

resource "azurerm_container_app" "postgres_registros" {
  name                         = var.postgres_registros_server_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "postgres-password"
    value = random_password.postgres_registros.result
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "postgres-registros"
      image  = var.postgres_image
      cpu    = var.postgres_cpu
      memory = var.postgres_memory

      env {
        name  = "POSTGRES_DB"
        value = var.postgres_registros_database_name
      }

      env {
        name  = "POSTGRES_USER"
        value = var.postgres_admin_username
      }

      env {
        name        = "POSTGRES_PASSWORD"
        secret_name = "postgres-password"
      }

      env {
        name  = "PGDATA"
        value = "/var/lib/postgresql/data/pgdata"
      }

      startup_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 15
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 30
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 5432
    exposed_port     = 5432
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "postgres-registros"
  })
}

resource "azurerm_container_app" "postgres_posicao" {
  name                         = var.postgres_posicao_server_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "postgres-password"
    value = random_password.postgres_posicao.result
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "postgres-posicao"
      image  = var.postgres_image
      cpu    = var.postgres_cpu
      memory = var.postgres_memory

      env {
        name  = "POSTGRES_DB"
        value = var.postgres_posicao_database_name
      }

      env {
        name  = "POSTGRES_USER"
        value = var.postgres_admin_username
      }

      env {
        name        = "POSTGRES_PASSWORD"
        secret_name = "postgres-password"
      }

      env {
        name  = "PGDATA"
        value = "/var/lib/postgresql/data/pgdata"
      }

      startup_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 15
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "TCP"
        port             = 5432
        initial_delay    = 30
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 5432
    exposed_port     = 5432
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "postgres-posicao"
  })
}

resource "azurerm_container_app" "redis" {
  name                         = var.redis_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name    = "redis"
      image   = var.redis_image
      cpu     = var.redis_cpu
      memory  = var.redis_memory
      command = ["redis-server"]
      args    = ["--appendonly", "yes", "--dir", "/data"]

      startup_probe {
        transport        = "TCP"
        port             = 6379
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "TCP"
        port             = 6379
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "TCP"
        port             = 6379
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }

    }
  }

  ingress {
    external_enabled = false
    target_port      = 6379
    exposed_port     = 6379
    transport        = "tcp"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "redis"
  })
}

resource "azurerm_container_app" "pgadmin" {
  name                         = var.pgadmin_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "pgadmin-password"
    value = random_password.pgadmin.result
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "pgadmin"
      image  = var.pgadmin_image
      cpu    = var.pgadmin_cpu
      memory = var.pgadmin_memory

      env {
        name  = "PGADMIN_DEFAULT_EMAIL"
        value = var.pgadmin_email
      }

      env {
        name        = "PGADMIN_DEFAULT_PASSWORD"
        secret_name = "pgadmin-password"
      }

      env {
        name  = "PGADMIN_LISTEN_PORT"
        value = "5050"
      }

      env {
        name  = "PGADMIN_DISABLE_POSTFIX"
        value = "true"
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/misc/ping"
        port             = 5050
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/misc/ping"
        port             = 5050
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/misc/ping"
        port             = 5050
        initial_delay    = 30
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 5050
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "pgadmin"
  })
}

resource "azurerm_container_app" "redisinsight" {
  name                         = var.redisinsight_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "redisinsight"
      image  = var.redisinsight_image
      cpu    = var.redisinsight_cpu
      memory = var.redisinsight_memory

      startup_probe {
        transport        = "TCP"
        port             = 5540
        initial_delay    = 15
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "TCP"
        port             = 5540
        initial_delay    = 15
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "TCP"
        port             = 5540
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }

    }
  }

  ingress {
    external_enabled = true
    target_port      = 5540
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "redisinsight"
  })
}

resource "azurerm_container_app" "rabbitmq" {
  name                         = var.rabbitmq_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "rabbitmq-password"
    value = random_password.rabbitmq.result
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "rabbitmq"
      image  = var.rabbitmq_image
      cpu    = var.rabbitmq_cpu
      memory = var.rabbitmq_memory

      env {
        name  = "RABBITMQ_DEFAULT_USER"
        value = var.rabbitmq_username
      }

      env {
        name        = "RABBITMQ_DEFAULT_PASS"
        secret_name = "rabbitmq-password"
      }

      env {
        name  = "RABBITMQ_MNESIA_BASE"
        value = "/var/lib/rabbitmq/mnesia"
      }

      env {
        name  = "RABBITMQ_LOG_BASE"
        value = "/var/lib/rabbitmq/log"
      }

      startup_probe {
        transport        = "TCP"
        port             = 5672
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "TCP"
        port             = 5672
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "TCP"
        port             = 5672
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 15672
    transport        = "http"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "rabbitmq"
  })
}

resource "azapi_update_resource" "rabbitmq_additional_ports" {
  type        = "Microsoft.App/containerApps@2025-07-01"
  resource_id = azurerm_container_app.rabbitmq.id

  body = {
    properties = {
      configuration = {
        secrets = [
          {
            name  = "rabbitmq-password"
            value = random_password.rabbitmq.result
          }
        ]
        ingress = {
          external      = true
          targetPort    = 15672
          transport     = "http"
          allowInsecure = false
          traffic = [
            {
              latestRevision = true
              weight         = 100
            }
          ]
          additionalPortMappings = [
            {
              exposedPort = 5672
              external    = false
              targetPort  = 5672
            },
            {
              exposedPort = 15692
              external    = false
              targetPort  = 15692
            }
          ]
        }
      }
    }
  }

  depends_on = [azurerm_container_app.rabbitmq]
}

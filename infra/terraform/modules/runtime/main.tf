resource "random_password" "jwt_secret" {
  length           = 32
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

resource "azurerm_container_registry" "main" {
  name                          = var.container_registry_name
  resource_group_name           = var.resource_group_name
  location                      = var.location
  sku                           = var.container_registry_sku
  admin_enabled                 = true
  public_network_access_enabled = true

  tags = merge(var.tags, {
    component = "acr"
  })
}

resource "azurerm_container_app" "registros_api" {
  count = var.enable_application_apps ? 1 : 0

  name                         = var.registros_api_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "jwt-secret"
    value = random_password.jwt_secret.result
  }

  secret {
    name  = "registros-conn"
    value = var.registros_connection_string
  }

  secret {
    name  = "rabbitmq-password"
    value = var.rabbitmq_password
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "registros-api"
      image  = var.registros_api_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.app_environment
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name        = "ConnectionStrings__Registros"
        secret_name = "registros-conn"
      }

      env {
        name  = "RabbitMQ__Host"
        value = var.rabbitmq_host
      }

      env {
        name  = "RabbitMQ__User"
        value = var.rabbitmq_username
      }

      env {
        name        = "RabbitMQ__Password"
        secret_name = "rabbitmq-password"
      }

      env {
        name        = "Jwt__Secret"
        secret_name = "jwt-secret"
      }

      env {
        name  = "Jwt__Issuer"
        value = var.jwt_issuer
      }

      env {
        name  = "Otlp__Endpoint"
        value = var.otlp_endpoint
      }

      env {
        name  = "RateLimit__PermitLimit"
        value = tostring(var.registros_ratelimit_permit)
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "registros-api"
  })
}

resource "azurerm_container_app" "posicao_api" {
  count = var.enable_application_apps ? 1 : 0

  name                         = var.posicao_api_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "jwt-secret"
    value = random_password.jwt_secret.result
  }

  secret {
    name  = "posicao-conn"
    value = var.posicao_connection_string
  }

  secret {
    name  = "redis-conn"
    value = var.redis_connection_string
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "posicao-api"
      image  = var.posicao_api_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.app_environment
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name        = "ConnectionStrings__Posicao"
        secret_name = "posicao-conn"
      }

      env {
        name        = "ConnectionStrings__Redis"
        secret_name = "redis-conn"
      }

      env {
        name        = "Jwt__Secret"
        secret_name = "jwt-secret"
      }

      env {
        name  = "Jwt__Issuer"
        value = var.jwt_issuer
      }

      env {
        name  = "Otlp__Endpoint"
        value = var.otlp_endpoint
      }

      env {
        name  = "RateLimit__PermitLimit"
        value = tostring(var.posicao_ratelimit_permit)
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = 8080
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "posicao-api"
  })
}

resource "azurerm_container_app" "posicao_processor" {
  count = var.enable_application_apps ? 1 : 0

  name                         = var.posicao_processor_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.main.admin_password
  }

  secret {
    name  = "posicao-conn"
    value = var.posicao_connection_string
  }

  secret {
    name  = "rabbitmq-password"
    value = var.rabbitmq_password
  }

  registry {
    server               = azurerm_container_registry.main.login_server
    username             = azurerm_container_registry.main.admin_username
    password_secret_name = "acr-password"
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "posicao-processor"
      image  = var.posicao_processor_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DOTNET_ENVIRONMENT"
        value = var.app_environment
      }

      env {
        name        = "ConnectionStrings__Posicao"
        secret_name = "posicao-conn"
      }

      env {
        name  = "RabbitMQ__Host"
        value = var.rabbitmq_host
      }

      env {
        name  = "RabbitMQ__User"
        value = var.rabbitmq_username
      }

      env {
        name        = "RabbitMQ__Password"
        secret_name = "rabbitmq-password"
      }

      env {
        name  = "Otlp__Endpoint"
        value = var.otlp_endpoint
      }

      env {
        name  = "Prometheus__Endpoint"
        value = var.prometheus_endpoint
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 8082
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "posicao-processor"
  })
}

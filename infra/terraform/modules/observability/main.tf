resource "random_password" "grafana_admin_password" {
  length           = 24
  special          = true
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

locals {
  prometheus_config = <<-EOT
    global:
      scrape_interval: 15s
      evaluation_interval: 15s

    scrape_configs:
      - job_name: "01 - API Registros"
        static_configs:
          - targets: ["${var.registros_api_name}"]
            labels:
              service: "api-registros"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "API Registros"

      - job_name: "02 - API Posicao"
        static_configs:
          - targets: ["${var.posicao_api_name}"]
            labels:
              service: "api-posicao"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "API Posicao"

      - job_name: "03 - Worker de Posicao"
        static_configs:
          - targets: ["${var.posicao_processor_name}"]
            labels:
              service: "worker-posicao"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "Worker de Posicao"

      - job_name: "04 - PostgreSQL Registros"
        static_configs:
          - targets: ["${var.postgres_registros_exporter_app_name}"]
            labels:
              service: "postgres-registros"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "PostgreSQL Registros"

      - job_name: "05 - PostgreSQL Posicao"
        static_configs:
          - targets: ["${var.postgres_posicao_exporter_app_name}"]
            labels:
              service: "postgres-posicao"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "PostgreSQL Posicao"

      - job_name: "06 - RabbitMQ Broker"
        scrape_interval: 5s
        static_configs:
          - targets: ["${var.rabbitmq_app_name}:15692"]
            labels:
              service: "rabbitmq-broker"
        metrics_path: /metrics/per-object
        relabel_configs:
          - target_label: instance
            replacement: "RabbitMQ Broker"

      - job_name: "07 - Redis Cache"
        static_configs:
          - targets: ["${var.redis_exporter_app_name}"]
            labels:
              service: "redis-cache"
        metrics_path: /metrics
        relabel_configs:
          - target_label: instance
            replacement: "Redis Cache"
  EOT

  grafana_datasource_config = <<-EOT
    apiVersion: 1

    datasources:
      - name: Prometheus
        type: prometheus
        access: proxy
        url: http://${var.prometheus_app_name}
        isDefault: true
        editable: false
  EOT

  grafana_dashboards_config   = file("${path.module}/../../../grafana/provisioning/dashboards/dashboards.yml")
  grafana_registros_dashboard = file("${path.module}/../../../grafana/provisioning/dashboards/registros.json")
  grafana_posicao_dashboard   = file("${path.module}/../../../grafana/provisioning/dashboards/posicao.json")
}

resource "azurerm_container_app" "jaeger" {
  name                         = var.jaeger_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "jaeger"
      image  = var.jaeger_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "COLLECTOR_OTLP_ENABLED"
        value = "true"
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/"
        port             = 16686
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/"
        port             = 16686
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/"
        port             = 16686
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 16686
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "jaeger"
  })
}

resource "azurerm_container_app" "prometheus" {
  name                         = var.prometheus_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name    = "prometheus"
      image   = var.prometheus_image
      cpu     = 0.25
      memory  = "0.5Gi"
      command = ["/bin/sh", "-c"]
      args = [
        "mkdir -p /etc/prometheus /prometheus ; printf '%s' \"$PROMETHEUS_CONFIG\" > /etc/prometheus/prometheus.yml ; /bin/prometheus --config.file=/etc/prometheus/prometheus.yml --storage.tsdb.path=/prometheus --storage.tsdb.retention.time=${var.prometheus_retention}"
      ]

      env {
        name  = "PROMETHEUS_CONFIG"
        value = local.prometheus_config
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/-/healthy"
        port             = 9090
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/-/ready"
        port             = 9090
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/-/healthy"
        port             = 9090
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 9090
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "prometheus"
  })
}

resource "azurerm_container_app" "grafana" {
  name                         = var.grafana_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "grafana-admin-password"
    value = random_password.grafana_admin_password.result
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name    = "grafana"
      image   = var.grafana_image
      cpu     = 0.25
      memory  = "0.5Gi"
      command = ["/bin/sh", "-c"]
      args = [
        "mkdir -p /etc/grafana/provisioning/datasources /etc/grafana/provisioning/dashboards ; printf '%s' \"$GRAFANA_DATASOURCE_YML\" > /etc/grafana/provisioning/datasources/prometheus.yml ; printf '%s' \"$GRAFANA_DASHBOARDS_YML\" > /etc/grafana/provisioning/dashboards/dashboards.yml ; printf '%s' \"$GRAFANA_REGISTROS_JSON\" > /etc/grafana/provisioning/dashboards/registros.json ; printf '%s' \"$GRAFANA_POSICAO_JSON\" > /etc/grafana/provisioning/dashboards/posicao.json ; /run.sh"
      ]

      env {
        name  = "GF_SECURITY_ADMIN_USER"
        value = var.grafana_admin_username
      }

      env {
        name        = "GF_SECURITY_ADMIN_PASSWORD"
        secret_name = "grafana-admin-password"
      }

      env {
        name  = "GF_USERS_ALLOW_SIGN_UP"
        value = "false"
      }

      env {
        name  = "GRAFANA_DATASOURCE_YML"
        value = local.grafana_datasource_config
      }

      env {
        name  = "GRAFANA_DASHBOARDS_YML"
        value = local.grafana_dashboards_config
      }

      env {
        name  = "GRAFANA_REGISTROS_JSON"
        value = local.grafana_registros_dashboard
      }

      env {
        name  = "GRAFANA_POSICAO_JSON"
        value = local.grafana_posicao_dashboard
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/api/health"
        port             = 3000
        initial_delay    = 30
        interval_seconds = 10
        timeout          = 10
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/api/health"
        port             = 3000
        initial_delay    = 20
        interval_seconds = 10
        timeout          = 10
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/api/health"
        port             = 3000
        initial_delay    = 40
        interval_seconds = 30
        timeout          = 10
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = 3000
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "grafana"
  })
}

resource "azurerm_container_app" "postgres_registros_exporter" {
  name                         = var.postgres_registros_exporter_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "postgres-registros-password"
    value = var.postgres_registros_password
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "pgexp-registros"
      image  = var.postgres_exporter_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DATA_SOURCE_URI"
        value = "${var.postgres_registros_hostname}:5432/${var.postgres_registros_database_name}?sslmode=disable"
      }

      env {
        name  = "DATA_SOURCE_USER"
        value = var.postgres_admin_username
      }

      env {
        name        = "DATA_SOURCE_PASS"
        secret_name = "postgres-registros-password"
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 9187
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "postgres-exporter-registros"
  })
}

resource "azurerm_container_app" "postgres_posicao_exporter" {
  name                         = var.postgres_posicao_exporter_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  secret {
    name  = "postgres-posicao-password"
    value = var.postgres_posicao_password
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "pgexp-posicao"
      image  = var.postgres_exporter_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "DATA_SOURCE_URI"
        value = "${var.postgres_posicao_hostname}:5432/${var.postgres_posicao_database_name}?sslmode=disable"
      }

      env {
        name  = "DATA_SOURCE_USER"
        value = var.postgres_admin_username
      }

      env {
        name        = "DATA_SOURCE_PASS"
        secret_name = "postgres-posicao-password"
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9187
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 9187
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "postgres-exporter-posicao"
  })
}

resource "azurerm_container_app" "redis_exporter" {
  name                         = var.redis_exporter_app_name
  container_app_environment_id = var.container_app_environment_id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"
  workload_profile_name        = "Consumption"

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = "redis-exporter"
      image  = var.redis_exporter_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "REDIS_ADDR"
        value = "redis://${var.redis_hostname}:6379"
      }

      startup_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9121
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      readiness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9121
        initial_delay    = 10
        interval_seconds = 10
        timeout          = 5
      }

      liveness_probe {
        transport        = "HTTP"
        path             = "/metrics"
        port             = 9121
        initial_delay    = 20
        interval_seconds = 30
        timeout          = 5
      }
    }
  }

  ingress {
    external_enabled = false
    target_port      = 9121
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  tags = merge(var.tags, {
    component = "redis-exporter"
  })
}

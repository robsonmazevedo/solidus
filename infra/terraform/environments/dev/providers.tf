terraform {
  required_version = "~> 1.14.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.68.0"
    }
    azapi = {
      source  = "Azure/azapi"
      version = "~> 2.4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.8.0"
    }
  }

  backend "azurerm" {}
}

provider "azapi" {
}

provider "azurerm" {
  features {}

  resource_provider_registrations = "none"
  resource_providers_to_register = [
    "Microsoft.App",
    "Microsoft.OperationalInsights",
    "Microsoft.Storage",
    "Microsoft.ContainerRegistry"
  ]
  storage_use_azuread = true
}

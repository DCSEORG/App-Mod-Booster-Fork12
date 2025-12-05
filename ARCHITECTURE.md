# Expense Management System - Architecture Diagram

## Azure Services Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Azure Cloud                                    │
│                                                                           │
│  ┌──────────────────────┐                                               │
│  │   User Browser       │                                               │
│  └──────────┬───────────┘                                               │
│             │                                                            │
│             │ HTTPS                                                      │
│             ▼                                                            │
│  ┌──────────────────────┐         ┌──────────────────────┐             │
│  │   App Service        │◄────────┤  User-Assigned       │             │
│  │   (Linux, .NET 8)    │         │  Managed Identity    │             │
│  │                      │         └──────────┬───────────┘             │
│  │  - Razor Pages UI    │                    │                          │
│  │  - REST APIs         │                    │ Authenticates            │
│  │  - Swagger Docs      │                    │                          │
│  │  - Chat UI (optional)│                    │                          │
│  └──────────┬───────────┘                    │                          │
│             │                                 │                          │
│             │                                 │                          │
│             │                                 ▼                          │
│             │                    ┌────────────────────────┐             │
│             │                    │   Azure SQL Database   │             │
│             └───────────────────►│                        │             │
│              Managed Identity    │  - Northwind DB        │             │
│              Authentication      │  - Entra ID Auth Only  │             │
│                                  │  - Stored Procedures   │             │
│                                  └────────────────────────┘             │
│                                                                           │
│  ┌─────────────────────── Optional GenAI Components ──────────────────┐ │
│  │                                                                      │ │
│  │  ┌──────────────────────┐         ┌──────────────────────┐        │ │
│  │  │  Azure OpenAI        │         │  Azure Cognitive     │        │ │
│  │  │                      │         │  Search (AI Search)  │        │ │
│  │  │  - GPT-4o Model      │         │                      │        │ │
│  │  │  - Function Calling  │         │  - RAG Support       │        │ │
│  │  │  - Sweden Central    │         │  - Vector Search     │        │ │
│  │  └──────────────────────┘         └──────────────────────┘        │ │
│  │             ▲                                 ▲                     │ │
│  │             │                                 │                     │ │
│  │             └─────────────┬───────────────────┘                     │ │
│  │                           │                                         │ │
│  │                  Managed Identity Access                            │ │
│  └──────────────────────────────────────────────────────────────────────┘
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘
```

## Component Descriptions

### Core Components (Always Deployed)

1. **App Service (Linux, .NET 8)**
   - Hosts the ASP.NET Core Razor Pages application
   - Provides REST APIs for expense management
   - Includes Swagger documentation
   - Uses User-Assigned Managed Identity for authentication

2. **User-Assigned Managed Identity**
   - Provides secure, password-less authentication
   - Used by App Service to access Azure SQL
   - Used by App Service to access Azure OpenAI (when deployed)

3. **Azure SQL Database**
   - Stores expense management data
   - Configured with Entra ID (Azure AD) authentication only
   - No SQL authentication allowed (security best practice)
   - Uses stored procedures for all data operations

### Optional Components (deploy-with-chat.sh)

4. **Azure OpenAI**
   - Provides GPT-4o model for AI chat functionality
   - Located in Sweden Central for optimal availability
   - Supports function calling for database interactions
   - Accessed via Managed Identity (no API keys)

5. **Azure Cognitive Search (AI Search)**
   - Enables Retrieval-Augmented Generation (RAG)
   - Provides vector search capabilities
   - Accessed via Managed Identity

## Security Features

- **No passwords or API keys** - All authentication uses Managed Identity
- **Entra ID only authentication** for Azure SQL
- **HTTPS enforced** on App Service
- **Firewall rules** for SQL Server
- **Role-based access** for managed identity

## Deployment Options

### Option 1: Basic Deployment (deploy.sh)
Deploys core components:
- App Service + Managed Identity
- Azure SQL Database
- Application code

### Option 2: Full Deployment (deploy-with-chat.sh)
Deploys everything including:
- All core components
- Azure OpenAI
- Azure Cognitive Search
- AI Chat UI

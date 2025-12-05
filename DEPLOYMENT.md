# Deployment Guide - Expense Management System

This guide explains how to deploy the modernized Expense Management System to Azure.

## Prerequisites

- Azure subscription
- Azure CLI installed and logged in (`az login`)
- Bash shell (Linux, macOS, or Windows WSL/Git Bash)
- Python 3.x with pip
- Permissions to create resources in Azure
- ODBC Driver 18 for SQL Server

### Installing ODBC Driver (if not present)

**Ubuntu/Debian:**
```bash
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list
sudo apt-get update
sudo ACCEPT_EULA=Y apt-get install -y msodbcsql18
```

**macOS:**
```bash
brew tap microsoft/mssql-release https://github.com/Microsoft/homebrew-mssql-release
brew update
brew install msodbcsql18
```

## Deployment Options

### Option 1: Basic Deployment (without AI Chat)

Deploy the core application with App Service, SQL Database, and Managed Identity:

```bash
bash deploy.sh
```

This will deploy:
- Azure App Service (Linux, .NET 8)
- Azure SQL Database with Entra ID authentication
- User-Assigned Managed Identity
- Expense Management web application
- REST APIs with Swagger documentation

**Deployment time:** ~10-15 minutes

### Option 2: Full Deployment (with AI Chat)

Deploy everything including Azure OpenAI for AI-powered chat:

```bash
bash deploy-with-chat.sh
```

This deploys everything from Option 1, plus:
- Azure OpenAI with GPT-4o model
- Azure Cognitive Search (AI Search)
- AI Chat UI with function calling
- RAG (Retrieval-Augmented Generation) support

**Deployment time:** ~15-20 minutes

## What Happens During Deployment

Both scripts follow these steps:

1. **Create Resource Group** - Creates a new resource group in UK South (or specified region)

2. **Deploy Infrastructure** - Uses Bicep templates to deploy:
   - App Service Plan and App Service
   - User-Assigned Managed Identity
   - Azure SQL Server and Database
   - (Optional) Azure OpenAI and Cognitive Search

3. **Configure SQL Server** - Waits for SQL Server readiness and:
   - Adds firewall rules for Azure services
   - Adds your current IP to firewall

4. **Import Database Schema** - Uses Python script to:
   - Import the Northwind database schema
   - Create tables, seed data
   - Execute using Azure AD authentication

5. **Configure Managed Identity** - Grants the managed identity:
   - Database reader/writer roles
   - Execute permissions on stored procedures

6. **Deploy Stored Procedures** - Creates all stored procedures for data operations

7. **Configure App Service** - Sets environment variables:
   - SQL connection string (with Managed Identity)
   - Managed Identity Client ID
   - (Optional) OpenAI endpoint and model name

8. **Deploy Application** - Uploads app.zip to App Service

## Post-Deployment

After successful deployment, you'll see output like:

```
===================================
Deployment Complete!
===================================

Your application is now available at:
  Main App: https://app-expensemgmt-xxxx.azurewebsites.net/Index
  Swagger API Docs: https://app-expensemgmt-xxxx.azurewebsites.net/swagger
  AI Chat UI: https://app-expensemgmt-xxxx.azurewebsites.net/chatui (if deployed with chat)
```

### Important Notes

1. **URL Access**: Navigate to `/Index` not just the root URL to view the application
2. **Startup Time**: Allow 2-3 minutes for the application to fully start after deployment
3. **Chat UI**: If you deployed without GenAI, the chat UI will display dummy responses

## Application Features

### Main Application (/Index)

- **View Expenses**: See all expenses with filtering
- **Add Expense**: Create new expense entries
- **Submit Expenses**: Submit draft expenses for approval
- **Approve Expenses**: Managers can approve/reject expenses

### REST APIs (/swagger)

Available endpoints:
- `GET /api/expenses` - List all expenses
- `GET /api/expenses/{id}` - Get specific expense
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense
- `GET /api/categories` - List expense categories
- `GET /api/users` - List users

### AI Chat UI (/chatui) - Optional

When deployed with GenAI services:
- Natural language queries about expenses
- Create expenses via chat
- Get summaries and insights
- Function calling to interact with database

## Running Locally

To run the application locally with your Azure credentials:

1. Ensure you're logged into Azure:
```bash
az login
```

2. Update `ExpenseManagementApp/appsettings.json` connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Database=Northwind;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

3. Run the application:
```bash
cd ExpenseManagementApp
dotnet run
```

4. Navigate to `https://localhost:5001/Index`

## Troubleshooting

### Deployment fails with "ODBC Driver not found"

Install ODBC Driver 18 for SQL Server (see Prerequisites section)

### Cannot connect to SQL Server

- Check firewall rules allow your IP
- Verify you're logged into Azure CLI with `az account show`
- Wait 30-60 seconds for SQL Server to be fully ready

### Application shows database errors

- Check App Service application settings have correct connection string
- Verify Managed Identity has been granted database permissions
- Check App Service logs: `az webapp log tail --name <app-name> --resource-group <rg-name>`

### Chat UI shows dummy responses

- Verify you deployed with `deploy-with-chat.sh` not `deploy.sh`
- Check App Service settings include OpenAI endpoint configuration
- Allow 5-10 minutes for OpenAI deployment to complete

## Clean Up

To delete all resources:

```bash
az group delete --name rg-expensemgmt-demo --yes --no-wait
```

## Cost Estimates

**Basic Deployment (deploy.sh):**
- App Service (S1): ~£55/month
- Azure SQL (Basic): ~£4/month
- **Total: ~£59/month**

**Full Deployment (deploy-with-chat.sh):**
- App Service (S1): ~£55/month
- Azure SQL (Basic): ~£4/month
- Azure OpenAI (GPT-4o, S0): ~£0 + usage
- Cognitive Search (Basic): ~£60/month
- **Total: ~£119/month + OpenAI usage**

Note: These are estimates. Actual costs may vary based on usage and region.

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for a detailed architecture diagram and component descriptions.

## Security

This deployment follows Azure best practices:
- ✅ Managed Identity (no passwords or API keys)
- ✅ Entra ID authentication only for SQL
- ✅ HTTPS enforced
- ✅ SQL firewall configured
- ✅ Least privilege access

## Support

For issues or questions, please open an issue in the GitHub repository.

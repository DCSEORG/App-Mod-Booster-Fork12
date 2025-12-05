#!/bin/bash
set -e

echo "==================================="
echo "Expense Management System Deployment (with GenAI Chat)"
echo "==================================="
echo ""

# Get current user for SQL admin
ADMIN_USER=$(az account show --query user.name -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo "Deploying as: $ADMIN_USER"
echo "Object ID: $ADMIN_OBJECT_ID"
echo ""

# Prompt for resource group name or use default
read -p "Enter resource group name [rg-expensemgmt-demo]: " RESOURCE_GROUP
RESOURCE_GROUP=${RESOURCE_GROUP:-rg-expensemgmt-demo}

# Prompt for location or use default
read -p "Enter location [uksouth]: " LOCATION
LOCATION=${LOCATION:-uksouth}

echo ""
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

echo "Deploying infrastructure with GenAI services (this may take several minutes)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file bicep/main.bicep \
    --parameters adminObjectId=$ADMIN_OBJECT_ID adminLogin="$ADMIN_USER" deployGenAI=true \
    --output json)

echo "✓ Infrastructure deployed successfully!"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.searchEndpoint.value')

echo "Deployed Resources:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_FQDN"
echo "  Database: $DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Wait for SQL Server to be fully ready
echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Install Python dependencies
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual values
echo "Updating database connection scripts..."
sed -i.bak "s/REPLACE_SERVER_NAME/$SQL_SERVER_NAME/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/REPLACE_SERVER_NAME/$SQL_SERVER_NAME/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/REPLACE_SERVER_NAME/$SQL_SERVER_NAME/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo "Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo "Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Configure App Service settings including OpenAI
echo "Configuring App Service settings..."
az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "ConnectionStrings__DefaultConnection=Server=tcp:$SQL_SERVER_FQDN,1433;Database=$DATABASE_NAME;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
        "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
        "CognitiveSearch__Endpoint=$SEARCH_ENDPOINT" \
    --output none

echo "✓ App Service configured with GenAI settings"
echo ""

# Deploy application code
echo "Deploying application code..."
if [ ! -f "app.zip" ]; then
    echo "Error: app.zip not found. Please ensure the application has been built and packaged."
    exit 1
fi

az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip \
    --output none

echo "✓ Application deployed successfully!"
echo ""

echo "==================================="
echo "Deployment Complete!"
echo "==================================="
echo ""
echo "Your application is now available at:"
echo "  Main App: $APP_SERVICE_URL/Index"
echo "  AI Chat UI: $APP_SERVICE_URL/chatui"
echo "  Swagger API Docs: $APP_SERVICE_URL/swagger"
echo ""
echo "Note: It may take a few minutes for the app to fully start."
echo "      Navigate to /Index (not just the root URL) to view the expense management interface."
echo "      Navigate to /chatui to interact with the AI assistant."
echo ""
echo "To run the app locally with your Azure AD credentials, use:"
echo "  az login"
echo "  Then update appsettings.json connection string to use 'Authentication=Active Directory Default'"
echo ""

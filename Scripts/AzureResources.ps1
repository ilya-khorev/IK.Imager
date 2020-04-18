# To run with PowerShell execute the following command: 
# .\AzureResources.ps1
# If Azure module is not installed, run the command below as administrator:
# Install-Module -Name Az -AllowClobber

# Output all available subscriptions
Get-AzContext -ListAvailable

# Specify your resource group, subscription, and location here
$resourceGroup = "TestResourceGroup"
$location = "westeurope"
$subscriptionName = ""

# Selecting an appropriate subscription
Set-AzContext -Subscription $subscriptionName

# Creating blob storage
$accountName = "ikimagesstorageaccount"
New-AzStorageAccount -ResourceGroupName $resourceGroup `
  -Name $accountName `
  -Location $location `
  -SkuName Standard_LRS `
  -Kind StorageV2

# Creating Cosmos DB
$accountName = "imagemetadatadb"
$apiKind = "GlobalDocumentDB"
#New-AzCosmosDBAccount -ResourceGroupName $resourceGroup `
      -Location $location -Name $accountName `
      -ApiKind $apiKind -EnableAutomaticFailover:$true `

# Creating Service Bus
# Query to see if the namespace currently exists
$namespace = "ikimagesmyservicebus"
$CurrentNamespace = Get-AzServiceBusNamespace -ResourceGroup $resourceGroup -NamespaceName $namespace
# Check if the namespace already exists or needs to be created
if ($CurrentNamespace)
{
    Write-Host "The namespace $Namespace already exists in the $location region:"
 	# Report what was found
 	Get-AzServiceBusNamespace -ResourceGroup $resourceGroup -NamespaceName $namespace
}
else
{
    Write-Host "The $Namespace namespace does not exist."
    Write-Host "Creating the $Namespace namespace in the $location region..."
    New-AzServiceBusNamespace -ResourceGroup $resourceGroup -NamespaceName $namespace -Location $location
    $CurrentNamespace = Get-AzServiceBusNamespace -ResourceGroup $resourceGroup -NamespaceName $namespace
    Write-Host "The $Namespace namespace in Resource Group $resourceGroup in the $location region has been successfully created."
}

#Creating App Insights
$appInsightsName = "imagesappinsights"
New-AzApplicationInsights -ResourceGroupName $resourceGroup `
  -Name $appInsightsName `
  -location $location `
  -Kind other 
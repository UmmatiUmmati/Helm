# .\create.ps1 -Namespace 'ummati-production'

[CmdletBinding()]
Param(
    [string]$Namespace
)

Write-Output "Creating Namespace $Namespace";
kubectl create namespace $Namespace
Write-Output '';

Write-Output "Creating Service Account tiller-$Namespace";
kubectl create serviceaccount "tiller-$Namespace" --namespace $Namespace
Write-Output '';

Write-Output "Creating Role tiller-manager-$Namespace"
kubectl create role "tiller-manager-$Namespace" --namespace $Namespace --verb=* --resource=*.,*.apps,*.batch,*.extensions
Write-Output '';

Write-Output "Creating Role Binding tiller-binding-$Namespace"
kubectl create rolebinding "tiller-binding-$Namespace" --namespace $Namespace --role="tiller-manager-$Namespace" --serviceaccount="$Namespace`:tiller-$Namespace"
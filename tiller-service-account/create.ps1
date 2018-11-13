# .\create.ps1 -namespace 'ummati-production'

[CmdletBinding()]
Param(
    [string]$namespace
)

Write-Output "Creating Namespace $namespace";
kubectl create namespace $namespace
Write-Output '';

Write-Output "Creating Service Account tiller-$namespace";
kubectl create serviceaccount "tiller-$namespace" --namespace $namespace
Write-Output '';

Write-Output 'Creating Role';
(Get-Content 'role.yaml').replace('${Namespace}', $namespace) | Set-Content 'role-output.yaml';
Get-Content role-output.yaml
Write-Output '';
kubectl apply --filename role-output.yaml
Write-Output '';

Write-Output 'Creating Role Binding';
(Get-Content 'role-binding.yaml').replace('${Namespace}', $namespace) | Set-Content 'role-binding-output.yaml';
Get-Content role-binding-output.yaml
Write-Output '';
kubectl apply --filename role-binding-output.yaml
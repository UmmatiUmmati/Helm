# .\create.sh "ummati-production"

Namespace="$1"

echo "Creating Namespace $Namespace"
kubectl create namespace $Namespace
echo

echo "Creating Service Account tiller-$Namespace"
kubectl create serviceaccount "tiller-$Namespace" --namespace $Namespace
echo

echo 'Creating Role tiller-manager-$Namespace'
kubectl create role "tiller-manager-$Namespace" --namespace $Namespace --verb=* --resource=*.,*.apps,*.batch,*.extensions
echo

echo 'Creating Role Binding tiller-binding-$Namespace'
kubectl create rolebinding "tiller-binding-$Namespace" --namespace $Namespace --role="tiller-manager-$Namespace" --serviceaccount="$Namespace:tiller-$Namespace"
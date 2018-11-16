# .\create.sh "foo"

Namespace="$1"

echo "Creating Namespace $Namespace"
kubectl create namespace $Namespace --output yaml
echo

echo "Creating Service Account tiller-$Namespace"
kubectl create serviceaccount "tiller-$Namespace" --namespace $Namespace --output yaml
echo

echo 'Creating Role tiller-role-$Namespace'
kubectl create role "tiller-role-$Namespace" /
    --namespace $Namespace /
    --verb=* /
    --resource=*.,*.apps,*.batch,*.extensions /
    --output yaml
echo

echo 'Creating Role Binding tiller-rolebinding-$Namespace'
kubectl create rolebinding "tiller-rolebinding-$Namespace" /
    --namespace $Namespace /
    --role="tiller-role-$Namespace" /
    --serviceaccount="$Namespace:tiller-$Namespace" /
    --output yaml
# .\create.sh "ummati-production"

export Namespace="$1"

echo "Creating Namespace $Namespace"
kubectl create namespace $Namespace
echo

echo "Creating Service Account tiller-$Namespace"
kubectl create serviceaccount "tiller-$Namespace" --namespace $Namespace
echo

echo 'Creating Role'
envsubst < role.yaml
envsubst < role.yaml | kubectl apply --filename -
echo

echo 'Creating Role Binding'
envsubst < role-binding.yaml
envsubst < role-binding.yaml | kubectl apply --filename -
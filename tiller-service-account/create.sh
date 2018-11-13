# .\create.sh "ummati-production"

namespace="$1"

echo "Creating Namespace $namespace"
kubectl create namespace $namespace
echo

echo "Creating Service Account tiller-$namespace"
kubectl create serviceaccount "tiller-$namespace" --namespace $namespace
echo

echo 'Creating Role'
sed "s/\$namespace/$namespace/g" <role.yaml >role-output.yaml
cat role-output.yaml
echo
kubectl apply --filename role-output.yaml
echo

echo 'Creating Role Binding'
sed "s/\$namespace/$namespace/g" <role-binding.yaml >role-binding-output.yaml
cat role-binding-output.yaml
echo
kubectl apply --filename role-binding-output.yaml
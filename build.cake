var target = Argument("Target", "Default");
var version =
    HasArgument("Version") ? Argument<string>("Version") :
    TFBuild.IsRunningOnVSTS ? TFBuild.Environment.Build.Number :
    EnvironmentVariable("Version") != null ? EnvironmentVariable("Version") :
    "1.0.0";
var branch =
    HasArgument("Branch") ? Argument<string>("Branch") :
    TFBuild.IsRunningOnVSTS ? TFBuild.Environment.Repository.Branch :
    EnvironmentVariable("Branch") != null ? EnvironmentVariable("Branch") :
    "master";
var artefactsDirectory =
    HasArgument("ArtefactsDirectory") ? Directory(Argument<string>("ArtefactsDirectory")) :
    EnvironmentVariable("ArtefactsDirectory") != null ? Directory(EnvironmentVariable("ArtefactsDirectory")) :
    Directory("./Artefacts");
var azureSubscriptionId =
    HasArgument("AzureSubscriptionId") ? Argument<string>("AzureSubscriptionId") :
    EnvironmentVariable("AzureSubscriptionId") != null ? EnvironmentVariable("AzureSubscriptionId") :
    null;
var azureContainerRegistryName =
    HasArgument("AzureContainerRegistryName") ? Argument<string>("AzureContainerRegistryName") :
    EnvironmentVariable("AzureContainerRegistryName") != null ? EnvironmentVariable("AzureContainerRegistryName") :
    null;
var azureContainerRegistryUsername =
    HasArgument("AzureContainerRegistryUsername") ? Argument<string>("AzureContainerRegistryUsername") :
    EnvironmentVariable("AzureContainerRegistryUsername") != null ? EnvironmentVariable("AzureContainerRegistryUsername") :
    null;
var azureContainerRegistryPassword =
    HasArgument("AzureContainerRegistryPassword") ? Argument<string>("AzureContainerRegistryPassword") :
    EnvironmentVariable("AzureContainerRegistryPassword") != null ? EnvironmentVariable("AzureContainerRegistryPassword") :
    null;

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(artefactsDirectory);
        Information($"Cleaned {artefactsDirectory}");
    });

Task("Lint")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("lint")
                    .Append("Ummati")
                    .Append("--strict")
            });
    });

 Task("Build")
    .IsDependentOn("Lint")
    .Does(() =>
    {
        StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("package")
                    .Append("Ummati")
                    .Append("--dependency-update")
                    .AppendSwitch("--destination", artefactsDirectory.ToString())
                    .AppendSwitch("--version", branch == "master" ? version : $"{version}-{branch}")
            });
    });

Task("Push")
    .IsDependentOn("Build")
    .Does(() =>
    {
        foreach(var package in GetFiles("./**/*.tgz"))
        {
             StartProcess(
                 IsRunningOnWindows() ? Context.Tools.Resolve("az.cmd") : "az",
                 new ProcessSettings()
                 {
                     Arguments = new ProcessArgumentBuilder()
                        .Append("acr helm push")
                        .Append("--force")
                        .AppendSwitch("--subscription", azureSubscriptionId)
                        .AppendSwitch("--name", azureContainerRegistryName)
                        .AppendSwitch("--username", azureContainerRegistryUsername)
                        .AppendSwitch("--password", azureContainerRegistryPassword)
                        .AppendQuoted(package.ToString())
                 });
        }
    });

Task("Default")
    .IsDependentOn("Push");

RunTarget(target);
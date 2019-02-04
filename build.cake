var target = Argument("Target", "Default");
var version =
    HasArgument("Version") ? Argument<string>("Version") :
    EnvironmentVariable("Version") != null ? EnvironmentVariable("Version") :
    "1.0.0";
var artefactsDirectory =
    HasArgument("ArtefactsDirectory") ? Directory(Argument<string>("ArtefactsDirectory")) :
    EnvironmentVariable("ArtefactsDirectory") != null ? Directory(EnvironmentVariable("ArtefactsDirectory")) :
    Directory("./Artefacts");
var helmReleaseName =
    HasArgument("HelmReleaseName") ? Argument<string>("HelmReleaseName") :
    EnvironmentVariable("HelmReleaseName") != null ? EnvironmentVariable("HelmReleaseName") :
    "RELEASE-NAME";
var helmNamespace =
    HasArgument("HelmNamespace") ? Argument<string>("HelmNamespace") :
    EnvironmentVariable("HelmNamespace") != null ? EnvironmentVariable("HelmNamespace") :
    "NAMESPACE";
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
        CreateDirectory(artefactsDirectory);
        Information($"Cleaned {artefactsDirectory}");
    });

Task("Init")
    .Does(() =>
    {
        var exitCode = StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("init")
                    .Append("--client-only")
            });
        if (exitCode != 0 && !TFBuild.IsRunningOnVSTS)
        {
            throw new Exception($"helm init failed with exit code {exitCode}.");
        }
    });

Task("Update")
    .Does(() =>
    {
        var exitCode = StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("dependency")
                    .Append("update")
                    .Append("ummati")
            });
        if (exitCode != 0 && !TFBuild.IsRunningOnVSTS)
        {
            throw new Exception($"helm dependency update failed with exit code {exitCode}.");
        }
    });

Task("Lint")
    .Does(() =>
    {
        var exitCode = StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("lint")
                    .Append("ummati")
                    .Append("--strict")
            });
        if (exitCode != 0)
        {
            throw new Exception($"helm lint failed with exit code {exitCode}.");
        }
    });

Task("Version")
    .Does(() =>
    {
        var chartYamlFilePath = GetFiles("./**/Chart.yaml").First().ToString();
        var chartYaml = System.IO.File.ReadAllText(chartYamlFilePath);
        chartYaml.Replace("1.0.0", version);
        System.IO.File.WriteAllText(chartYamlFilePath, chartYaml);

        Information($"Version set to {version} in Chart.yaml");
    });

Task("Package")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .Does(() =>
    {
        var exitCode = StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("package")
                    .Append("ummati")
                    .AppendSwitch("--destination", MakeAbsolute(artefactsDirectory).ToString())
                    .AppendSwitch("--version", version)
            });
        if (exitCode != 0)
        {
            throw new Exception($"helm package failed with exit code {exitCode}.");
        }
    });

Task("Template")
    .IsDependentOn("Clean")
    .IsDependentOn("Update")
    .IsDependentOn("Lint")
    .Does(() =>
    {
        // helm template --output-dir ./***-final ./***
        var exitCode = StartProcess(
            "helm",
            new ProcessSettings()
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append("template")
                    .AppendSwitch("--name", helmReleaseName)
                    .AppendSwitch("--namespace", helmNamespace)
                    .AppendSwitch("--output-dir", MakeAbsolute(artefactsDirectory).ToString())
                    .Append("./ummati")
            });
        if (exitCode != 0)
        {
            throw new Exception($"helm template failed with exit code {exitCode}.");
        }
    });

Task("Push")
    .Does(() =>
    {
        foreach(var package in GetFiles("./**/*.tgz"))
        {
             var exitCode = StartProcess(
                 Context.Tools.Resolve(IsRunningOnWindows() ? "az.cmd" : "az"),
                 new ProcessSettings()
                 {
                     Arguments = new ProcessArgumentBuilder()
                        .Append("acr helm push")
                        .AppendSwitch("--name", azureContainerRegistryName)
                        .AppendSwitch("--username", azureContainerRegistryUsername)
                        .AppendSwitch("--password", azureContainerRegistryPassword)
                        .AppendQuoted(package.ToString())
                 });
            if (exitCode != 0)
            {
                throw new Exception($"acr helm push failed with exit code {exitCode}.");
            }
        }
    });

Task("Default")
    .IsDependentOn("Package");

RunTarget(target);
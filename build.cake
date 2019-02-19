var target = Argument("Target", "Default");
var packageVersion =
    HasArgument("PackageVersion") ? Argument<string>("PackageVersion") :
    EnvironmentVariable("PackageVersion") != null ? EnvironmentVariable("PackageVersion") :
    "1.0.0";
var artefactsDirectory =
    HasArgument("ArtefactsDirectory") ? Directory(Argument<string>("ArtefactsDirectory")) :
    EnvironmentVariable("ArtefactsDirectory") != null ? Directory(EnvironmentVariable("ArtefactsDirectory")) :
    Directory("./Artefacts");
var helmChartNames =
    (HasArgument("HelmChartName") ? Argument<string>("HelmChartName") :
    EnvironmentVariable("HelmChartName") != null ? EnvironmentVariable("HelmChartName") :
    "azure-aks,ummati").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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
        StartProcess(
            "helm",
            new ProcessArgumentBuilder()
                .Append("init")
                .Append("--client-only"));
    });

Task("Update")
    .Does(() =>
    {
        foreach (var helmChartName in helmChartNames)
        {
            StartProcess(
                "helm",
                new ProcessArgumentBuilder()
                    .Append("dependency")
                    .Append("update")
                    .Append(helmChartName));
        }
    });

Task("Lint")
    .Does(() =>
    {
        foreach (var helmChartName in helmChartNames)
        {
            StartProcess(
                "helm",
                new ProcessArgumentBuilder()
                    .Append("lint")
                    .Append(helmChartName)
                    .Append("--strict"));
        }
    });

Task("Version")
    .Does(() =>
    {
        var chartYamlFilePath = GetFiles("./**/Chart.yaml").First().ToString();
        var chartYaml = System.IO.File.ReadAllText(chartYamlFilePath);
        chartYaml.Replace("1.0.0", packageVersion);
        System.IO.File.WriteAllText(chartYamlFilePath, chartYaml);

        Information($"Version set to {packageVersion} in Chart.yaml");
    });

Task("Package")
    .IsDependentOn("Clean")
    .IsDependentOn("Version")
    .Does(() =>
    {
        foreach (var helmChartName in helmChartNames)
        {
            StartProcess(
                "helm",
                new ProcessArgumentBuilder()
                    .Append("package")
                    .Append(helmChartName)
                    .AppendSwitch("--destination", MakeAbsolute(artefactsDirectory).ToString())
                    .AppendSwitch("--version", packageVersion));
        }
    });

Task("Template")
    .IsDependentOn("Clean")
    .IsDependentOn("Update")
    .IsDependentOn("Lint")
    .Does(() =>
    {
        foreach (var helmChartName in helmChartNames)
        {
            StartProcess(
                "helm",
                new ProcessArgumentBuilder()
                    .Append("template")
                    .AppendSwitch("--name", helmReleaseName)
                    .AppendSwitch("--namespace", helmNamespace)
                    .AppendSwitch("--output-dir", MakeAbsolute(artefactsDirectory).ToString())
                    .Append($"./{helmChartName}"));
        }
    });

Task("Push")
    .Does(() =>
    {
        foreach(var package in GetFiles($"{artefactsDirectory}/**/*.tgz"))
        {
            StartProcess(
                Context.Tools.Resolve(IsRunningOnWindows() ? "az.cmd" : "az").ToString(),
                new ProcessArgumentBuilder()
                    .Append("acr helm push")
                    .AppendSwitch("--name", azureContainerRegistryName)
                    .AppendSwitch("--username", azureContainerRegistryUsername)
                    .AppendSwitch("--password", azureContainerRegistryPassword)
                    .AppendQuoted(package.ToString()));
        }
    });

Task("Default")
    .IsDependentOn("Package");

RunTarget(target);

public void StartProcess(string processName, ProcessArgumentBuilder builder)
{
    var command = $"{processName} {builder.RenderSafe()}";
    Information($"Executing: {command}");
    var exitCode = StartProcess(
        processName,
        new ProcessSettings()
        {
            Arguments = builder
        });
    if (exitCode != 0 && !TFBuild.IsRunningOnVSTS)
    {
        throw new Exception($"'{command}' failed with exit code {exitCode}.");
    }
}
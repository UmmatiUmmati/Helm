var target = Argument("Target", "Default");
var packageVersion = GetArgumentValue("PackageVersion", "1.0.0");
var artefactsDirectory = Directory(GetArgumentValue("ArtefactsDirectory", "./Artefacts"));
var helmChartNames = GetArgumentValue("HelmChartName", "azure-aks,ummati").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
var helmReleaseName = GetArgumentValue("HelmReleaseName", "RELEASE-NAME");
var helmNamespace = GetArgumentValue("HelmNamespace", "NAMESPACE");
var azureSubscriptionId = GetArgumentValue("AzureSubscriptionId", null);
var azureContainerRegistryName = GetArgumentValue("AzureContainerRegistryName", null);
var azureContainerRegistryUsername = GetArgumentValue("AzureContainerRegistryUsername", null);
var azureContainerRegistryPassword = GetArgumentValue("AzureContainerRegistryPassword", null);

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

public string GetArgumentValue(string name, string defaultValue) =>
    HasArgument(name) ? Argument<string>(name) :
    EnvironmentVariable(name) != null ? EnvironmentVariable(name) :
    defaultValue;

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
    if (exitCode != 0 && !TFBuild.IsRunningOnAzurePipelinesHosted)
    {
        throw new Exception($"'{command}' failed with exit code {exitCode}.");
    }
}
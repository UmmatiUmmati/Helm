#addin "Syncromatics.Cake.Helm"

using Cake.Helm.Lint;
using Cake.Helm.Package;

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
        HelmLint(
            new HelmLintSettings()
            {
                Strict = true
            },
            "Ummati");
    });

 Task("Build")
    .IsDependentOn("Lint")
    .Does(() =>
    {
        HelmPackage(
            new HelmPackageSettings()
            {
                DependencyUpdate = true,
                Destination = artefactsDirectory.ToString(),
                Version = branch == "master" ? version : $"{version}-{branch}"
            },
            "Ummati");
    });

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
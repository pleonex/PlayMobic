#r "nuget: Cake.Frosting.PleOps.Recipe, 0.9.0-preview.48"

using Cake.Core;
using Cake.Frosting;
using Cake.Frosting.PleOps.Recipe;
using Cake.Frosting.PleOps.Recipe.Dotnet;

return new CakeHost()
    .AddAssembly(typeof(BuildLifetime).Assembly)
    .AddAssembly(typeof(Cake.Frosting.PleOps.Recipe.PleOpsBuildContext).Assembly)
    .UseContext<PleOpsBuildContext>()
    .UseLifetime<BuildLifetime>()
    .Run(Args);

public sealed class BuildLifetime : FrostingLifetime<PleOpsBuildContext>
{
    public override void Setup(PleOpsBuildContext context, ISetupContext info)
    {
        // HERE you can set default values overridable by command-line
        context.WarningsAsErrors = false;
        context.DotNetContext.CoverageTarget = 0;
        context.DotNetContext.ApplicationProjects.Add(new ProjectPublicationInfo(
            "./src/PlayMobic.Tool", new[] { "win-x64", "linux-x64", "osx-x64" }, "net6.0"));

        // Update build parameters from command line arguments.
        context.ReadArguments();

        // HERE you can force values non-overridable.
        context.DotNetContext.PreviewNuGetFeed = "https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json";
        context.DotNetContext.StableNuGetFeed = "https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json";

        // Print the build info to use.
        context.Print();
    }

    public override void Teardown(PleOpsBuildContext context, ITeardownContext info)
    {
        // Save the info from the existing artifacts for the next execution (e.g. deploy job)
        context.DeliveriesContext.Save();
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Common.SetGitVersionTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Dotnet.BuildTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Dotnet.TestTask))]
public sealed class DefaultTask : FrostingTask
{
}

[TaskName("CI-Build")]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Common.SetGitVersionTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Common.CleanArtifactsTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.GitHub.ExportReleaseNotesTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Dotnet.DotnetTasks.PrepareProjectBundlesTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.DocFx.DocFxTasks.PrepareProjectBundlesTask))]
public sealed class CIBuildTask : FrostingTask
{
}

[TaskName("CI-Deploy")]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Common.SetGitVersionTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.Dotnet.DotnetTasks.DeployProjectTask))]
[IsDependentOn(typeof(Cake.Frosting.PleOps.Recipe.GitHub.UploadReleaseBinariesTask))]
public sealed class CIDeployTask : FrostingTask
{
}

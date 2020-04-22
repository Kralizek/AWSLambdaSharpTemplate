#tool "nuget:?package=ReportGenerator&version=4.0.5"
#tool "nuget:?package=JetBrains.dotCover.CommandLineTools&version=2019.3.4"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=NuGet.CommandLine&version=4.9.2"

#addin "Cake.FileHelpers"

var target = Argument("Target", "Full");

Setup<BuildState>(_ => 
{
    var state = new BuildState
    {
        Paths = new BuildPaths
        {
            SolutionFile = MakeAbsolute(File("./AWSLambdaSharpTemplate.sln"))
        }
    };

    CleanDirectory(state.Paths.OutputFolder);

    return state;
});

Task("Version")
    .Does<BuildState>(state =>
{
    var version = GitVersion();

    var packageVersion = version.SemVer;
    var buildVersion = $"{version.FullSemVer}+{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

    state.Version = new VersionInfo
    {
        PackageVersion = packageVersion,
        BuildVersion = buildVersion
    };


    Information($"Package version: {state.Version.PackageVersion}");
    Information($"Build version: {state.Version.BuildVersion}");

    if (BuildSystem.IsRunningOnAppVeyor)
    {
        AppVeyor.UpdateBuildVersion(state.Version.BuildVersion);
    }
});


Task("Restore")
    .Does<BuildState>(state =>
{
    var settings = new DotNetCoreRestoreSettings
    {

    };

    DotNetCoreRestore(state.Paths.SolutionFile.ToString(), settings);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does<BuildState>(state => 
{
    var settings = new DotNetCoreBuildSettings
    {
        Configuration = "Debug",
        NoRestore = true
    };

    DotNetCoreBuild(state.Paths.SolutionFile.ToString(), settings);
});


Task("RunTests")
    .IsDependentOn("Build")
    .Does<BuildState>(state => 
{
    var projectFiles = GetFiles($"{state.Paths.TestFolder}/**/Tests.*.csproj");

    bool success = true;

    foreach (var file in projectFiles)
    {
        try
        {
            Information($"Testing {file.GetFilenameWithoutExtension()}");

            var testResultFile = state.Paths.TestOutputFolder.CombineWithFilePath(file.GetFilenameWithoutExtension() + ".trx");
            var coverageResultFile = state.Paths.TestOutputFolder.CombineWithFilePath(file.GetFilenameWithoutExtension() + ".dcvr");

            var projectFile = MakeAbsolute(file).ToString();

            var dotCoverSettings = new DotCoverCoverSettings()
                                    .WithFilter("+:Kralizek.*")
                                    .WithFilter("-:Tests*")
                                    .WithFilter("-:TestUtils");

            var settings = new DotNetCoreTestSettings
            {
                NoBuild = true,
                NoRestore = true,
                Logger = $"trx;LogFileName={testResultFile.FullPath}",
                Filter = "TestCategory!=External"
            };

            DotCoverCover(c => c.DotNetCoreTest(projectFile, settings), coverageResultFile, dotCoverSettings);
        }
        catch (Exception ex)
        {
            Error($"There was an error while executing the tests: {file.GetFilenameWithoutExtension()}", ex);
            success = false;
        }

        Information("");
    }
    
    if (!success)
    {
        throw new CakeException("There was an error while executing the tests");
    }
});

Task("MergeCoverageResults")
    .IsDependentOn("RunTests")
    .Does<BuildState>(state =>
{
    Information("Merging coverage files");
    var coverageFiles = GetFiles($"{state.Paths.TestOutputFolder}/*.dcvr");
    DotCoverMerge(coverageFiles, state.Paths.DotCoverOutputFile);
    DeleteFiles(coverageFiles);
});

Task("GenerateXmlReport")
    .IsDependentOn("MergeCoverageResults")
    .Does<BuildState>(state =>
{
    Information("Generating dotCover XML report");
    DotCoverReport(state.Paths.DotCoverOutputFile, state.Paths.DotCoverOutputFileXml, new DotCoverReportSettings 
    {
        ReportType = DotCoverReportType.DetailedXML
    });
});

Task("GenerateHtmlReport")
    .IsDependentOn("GenerateXmlReport")
    .Does<BuildState>(state =>
{
    Information("Executing ReportGenerator to generate HTML report");
    ReportGenerator(state.Paths.DotCoverOutputFileXml, state.Paths.ReportFolder, new ReportGeneratorSettings {
            ReportTypes = new[]{ReportGeneratorReportType.Html, ReportGeneratorReportType.Xml}
    });
});

Task("UploadTestsToAppVeyor")
    .IsDependentOn("RunTests")
    .WithCriteria(BuildSystem.IsRunningOnAppVeyor)
    .Does<BuildState>(state =>
{
    Information("Uploading test result files to AppVeyor");
    var testResultFiles = GetFiles($"{state.Paths.TestOutputFolder}/*.trx");

    foreach (var file in testResultFiles)
    {
        Information($"\tUploading {file.GetFilename()}");
        AppVeyor.UploadTestResults(file, AppVeyorTestResultsType.MSTest);
    }
});

Task("Test")
    .IsDependentOn("RunTests")
    .IsDependentOn("MergeCoverageResults")
    .IsDependentOn("GenerateXmlReport")
    .IsDependentOn("GenerateHtmlReport")
    .IsDependentOn("UploadTestsToAppVeyor");


Task("PackLibraries")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .Does<BuildState>(state =>
{
    var settings = new DotNetCorePackSettings
    {
        Configuration = "Release",
        NoRestore = true,
        OutputDirectory = state.Paths.OutputFolder,
        IncludeSymbols = true,
        ArgumentCustomization = args => args.Append($"-p:SymbolPackageFormat=snupkg -p:Version={state.Version.PackageVersion}")
    };

    DotNetCorePack(state.Paths.SolutionFile.ToString(), settings);
});


Task("PackTemplates")
    .IsDependentOn("Version")
    .Does<BuildState>(state => 
{
    ReplaceRegexInFiles("./templates/content/**/*.csproj", @"<PackageReference Include=""(Kralizek.Lambda.Template[\w\.]*)"" Version="".+"" />", $@"<PackageReference Include=""$1"" Version=""{state.Version.PackageVersion}"" />");

    var settings = new NuGetPackSettings
    {
        Version = state.Version.PackageVersion,
        OutputDirectory = state.Paths.OutputFolder
    };

    NuGetPack("./templates/Kralizek.Lambda.Templates.nuspec", settings);
});

Task("Pack")
    .IsDependentOn("PackLibraries")
    .IsDependentOn("PackTemplates");

Task("UploadPackagesToAppVeyor")
    .IsDependentOn("Pack")
    .WithCriteria(BuildSystem.IsRunningOnAppVeyor)
    .Does<BuildState>(state => 
{
    Information("Uploading packages");
    var files = GetFiles($"{state.Paths.OutputFolder}/*.nukpg");

    foreach (var file in files)
    {
        Information($"\tUploading {file.GetFilename()}");
        AppVeyor.UploadArtifact(file, new AppVeyorUploadArtifactsSettings {
            ArtifactType = AppVeyorUploadArtifactType.NuGetPackage,
            DeploymentName = "NuGet"
        });
    }
});

Task("Push")
    .IsDependentOn("UploadPackagesToAppVeyor");

Task("Full")
    .IsDependentOn("Version")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .IsDependentOn("Push");

RunTarget(target);

public class BuildState
{
    public VersionInfo Version { get; set; }

    public BuildPaths Paths { get; set; }
}

public class BuildPaths
{
    public FilePath SolutionFile { get; set; }

    public DirectoryPath SolutionFolder => SolutionFile.GetDirectory();

    public DirectoryPath TestFolder => SolutionFolder.Combine("tests");

    public DirectoryPath OutputFolder => SolutionFolder.Combine("outputs");

    public DirectoryPath TestOutputFolder => OutputFolder.Combine("tests");

    public DirectoryPath ReportFolder => TestOutputFolder.Combine("report");

    public FilePath DotCoverOutputFile => TestOutputFolder.CombineWithFilePath("coverage.dcvr");

    public FilePath DotCoverOutputFileXml => TestOutputFolder.CombineWithFilePath("coverage.xml");

    public FilePath OpenCoverResultFile => OutputFolder.CombineWithFilePath("OpenCover.xml");
}

public class VersionInfo
{
    public string PackageVersion { get; set; }

    public string BuildVersion {get; set; }
}

# 1.7.2
- Changed: coverlet.msbuild updated to 2.5.0
- Fixed: AddReadme template placeholders fill

# 1.7.1
- Fixed: AddReadme statuses section generation

# 1.7.0
- Changed: ReleaseNotes limited by 5 last versions by default
- Added: `HasEnvironmentVariableIgnoreCase`, `EnvironmentVariableIgnoreCase` 

# 1.6.0
- Added: `BuildDirectory` and `BuildSamples`
- Added: `GetReleaseNotes` from ChangeLog and `GetMarkdownParagraph`
- Changed: Added chaining for building and testing methods

# 1.5.0
- Added: `AddReadme` target (Adds filled readme), task added to `Init` task
- Added: `UpdateReadmeBadges` target (Updates statuses section in readme)
- Added KeyEqualityComparer for customizing key comparation

# 1.4.1
- Fixed: HasValue for ParamValue<T> where T is ValueType 

# 1.4.0
- Added: `SetEmptyValues` for ScriptParam to treat some values as NoValue
- Changed: `TestSourceLink` option value is true for CI servers because local builds are often not committed.
- Fixed: Bug "Default value for ParamValue treats as NoValue"

# 1.3.0
- Added: `CodeCoverage` task
- Added: Coverlet CodeCoverage. see `UseCoverlet`
- Added: Task `UploadCoverageReportsToCoveralls` to upload coverage results to coveralls.io
- Added: ScriptParam `COVERALLS_REPO_TOKEN` to target caveralls.io project
- Added: `CodeCoverage` and `UploadCoverageReportsToCoveralls` added to `Travis` task
- Added: Directory.Build.props for Tests
- Changed: `CopyPackagesToArtifacts` placed after `Build` task because `Test` and `CodeCoverage` can build projects with other build parameters

# 1.2.0
- Added: `PrintHeader` for header printing using Figlet, added `Header` param.
- Added: functional stuff
- Fixed: Value override for list params
- Changed: `PrintParams` prints the same info that on build

# 1.1.1
- Bugfix: fixed build.sh script path

# 1.1.0
- Added task `UploadTestResultsToAppVeyor` to `AppVeyor` task
- Added task `AddAppVeyorFile`
- New: `AddFileFromTemplate` supports options and can fill template from params
- Changes: `ArtifactsDir` now is in `RootDir` by default
- Changes: `TestResultsDir` is the child of `ArtifactsDir`
- Changes: `PackagesDir` is the child of `ArtifactsDir`
- Bugfix: `DefaultValue` ParamSource is now always at the and of GetValueChain

# 1.0.0
The first major version.
- Includes main tasks: Init, Default, Travis, AppVeyor
- Task `Init` runs: CreateProjectStructure, CheckOrDownloadGitIgnore, GitIgnoreAddCakeRule, CreateProjects, EditorConfig, SourceLink, CreateCommonProjectFile, AddTravisFile, AddCakeBootstrapFiles, AddChangeLog, AddStyleCop
- Task `Default` runs: Build, Test, CopyPackagesToArtifacts
- Task `Travis` runs: DoVersioning, Build, Test, CopyPackagesToArtifacts, UploadPackages
- Task `AppVeyor` runs: Build, Test
- All scripts builded on concepts:
    - ScriptArgs contains all script params
    - ScriptParam:
        - Can be initializes from command line args, environment variables
        - Can be initialized from attributes: DefaultValue, ScriptParamAttribute
        - Evaluates by conventions
        - Can contain list values
- Scripts splitted on several files
- Extended customization

# 1.0.0-beta.2
- VersionParam
- used NugetSource, removed old non list nuget_sourceX params

# 1.0.0-beta.1
## Breaking changes:
- Removed ScriptParamFactory - all factory methods moved to ScriptParam for simplicity
- AutoCreation script params with Initialize methods. Initialize creates script param, initializes param from attributes, sets default getters from arguments and from environment variables.
- Added overriden operator ```/``` that combines ```ScriptParam<DirectoryPath>``` with string and gets combined DirectoryPath

# 0.5.0
- Redesigned ScriptParam and ScriptArgs
- New Targets: AddTravisFile, CopyPackagesToArtifacts, UploadPackages, AddStyleCop
- Added versioning (Manual and for some CI)
- Added generate package on build
- Added build.sh to resources and AddCakeBootstrapFiles target
- Build and Test moved to build.cake
- Added many common util methods
- Scenarios moved from common
- DotNetPack with releaseNotes

# 0.4.0
- Common props created
- Added: fill common props from github 

# 0.3.0
- Create version.props
- GetVersionFromCommandLineArgs, ReadTemplate
- Idempotent CreateProjectStructure
- Versioning methods

# 0.2.0
- Vesion that works from nuget pakage
- Init target: CreateProjectStructure, GitIgnore, CreateProjects, EditorConfig, SourceLink

# 0.1.0
- Initial version

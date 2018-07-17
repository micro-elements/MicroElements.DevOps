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

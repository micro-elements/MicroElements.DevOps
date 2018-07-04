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

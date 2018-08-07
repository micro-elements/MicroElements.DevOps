# MicroElements.DevOps
DevOps scripts for CI and CD

## Statuses
[![License](https://img.shields.io/github/license/micro-elements/MicroElements.DevOps.svg)](https://raw.githubusercontent.com/micro-elements/MicroElements.DevOps/master/LICENSE)
[![NuGetVersion](https://img.shields.io/nuget/v/MicroElements.DevOps.svg)](https://www.nuget.org/packages/MicroElements.DevOps)
![NuGetDownloads](https://img.shields.io/nuget/dt/MicroElements.DevOps.svg)
[![MyGetVersion](https://img.shields.io/myget/micro-elements/v/MicroElements.DevOps.svg)](https://www.myget.org/feed/micro-elements/package/nuget/MicroElements.DevOps)

[![Travis](https://img.shields.io/travis/micro-elements/MicroElements.DevOps/master.svg?logo=travis)](https://travis-ci.org/micro-elements/MicroElements.DevOps)
[![AppVeyor](https://img.shields.io/appveyor/ci/petriashev/microelements-devops.svg?logo=appveyor)](https://ci.appveyor.com/project/petriashev/microelements-devops)
[![Coverage Status](https://img.shields.io/coveralls/micro-elements/MicroElements.DevOps.svg)](https://coveralls.io/r/micro-elements/MicroElements.DevOps)

[![Gitter](https://img.shields.io/gitter/room/micro-elements/MicroElements.DevOps.svg)](https://gitter.im/micro-elements/MicroElements.DevOps)

## Main features
- Based on CakeBuild
- Supported OS: Windows, Linux
- Supported CI: Travis, AppVeyor, Jenkins, any other
- C# project generation or initialization with CI and CD included
- Only two files: build.ps1 and build.sh. All other files can be generated
- No need to maintain your own build scripts
- You can customize and add own tasks if you need

## Getting started
### 1. Download bootstrap script
Open a new PowerShell window and run the following command.
```ps
Invoke-WebRequest https://raw.githubusercontent.com/micro-elements/MicroElements.DevOps/master/resources/build.ps1 -OutFile build.ps1
```

### 2. Initialize component
Run target `Init`
```ps
./build.ps1 -Target "Init"
```

### 3. Usage

#### Local build

Run target `Default`
```ps
./build.ps1 -Target "Default"
```

#### Travis CI

Add file .travis.yml to project
```yml
language: csharp
mono: none
dotnet: 2.1.300
os:
  - linux
before_script:
  - chmod a+x ./build.sh
script:
  - ./build.sh --target=Travis --verbosity=normal
```

#### Other CI
Run shell script: `./build.sh --target=Travis`

## Tasks
### Init
Initializes project structure and adds all needed files

Runs:
* [CreateProjectStructure](#CreateProjectStructure)
* [CheckOrDownloadGitIgnore](#CheckOrDownloadGitIgnore)
* [GitIgnoreAddCakeRule](#GitIgnoreAddCakeRule)
* [CreateProjects](#CreateProjects)
* [EditorConfig](#EditorConfig)
* [SourceLink](#SourceLink)
* [CreateCommonProjectFiles](#CreateCommonProjectFiles)
* [AddTravisFile](#AddTravisFile)
* [AddAppVeyorFile](#AddAppVeyorFile)
* [AddCakeBootstrapFiles](#AddCakeBootstrapFiles)
* [AddChangeLog](#AddChangeLog)
* [AddReadme](#AddReadme)
* [AddStyleCop](#AddStyleCop)

### Default
Builds projects and runs tests

Runs:
* [Build](#Build)
* [Test](#Test)
* [CopyPackagesToArtifacts](#CopyPackagesToArtifacts)

### Travis
Does versioning, builds projects, runs tests creates and uploads artifacts

Runs:
* [DoVersioning](#DoVersioning)
* [Build](#Build)
* [Test](#Test)
* [CopyPackagesToArtifacts](#CopyPackagesToArtifacts)
* [UploadPackages](#UploadPackages)

### AppVeyor
Builds, tests and uploads test results to appVeyor

Runs:
* [Build](#Build)
* [Test](#Test)
* [UploadTestResultsToAppVeyor](#UploadTestResultsToAppVeyor)

### CreateProjectStructure
TODO: all tasks

## Features

## Customize build
add cake.build file in root of your project
TODO: samples

# Concepts
- ScriptParam
- ScriptArgs
- Value chains
- Conventions

# ScriptParam
- props
- get value chain

# ScriptArgs
Param | Description | DefaulValue
---|---|---
SrcDir | Sources directory. Contains projects. | src
TODO: all params






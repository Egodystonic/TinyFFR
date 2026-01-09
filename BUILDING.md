# Building TinyFFR

## Windows

* Install .NET 10
* Install latest Visual Studio, ensure the **.NET Desktop Development** and **Desktop Development with C++** workloads are installed
* Install [CMake](https://cmake.org/)
* Open command prompt in `/ThirdParty/` folder
	* Run `dotnet run build_and_copy_all_third_party.cs`; takes 30mins to an hour
* Open `TinyFFR.slnx`, make sure `x64` configuration is selected, build all

## MacOS

* Install .NET 10
* Install IDE(s) of your choice, Rider + CLion recommended
* Install [CMake](https://cmake.org/) and [Ninja](https://ninja-build.org/)
	* Via homebrew: `brew install cmake ninja`
* Install xcode-select (if XCode itself is not already installed) (`xcode-select --install`)
* Open terminal in `/ThirdParty/` folder
	* Run `dotnet run build_and_copy_all_third_party.cs`; takes 30mins to an hour
* Open terminal in `/TinyFFR.Native/build/` folder
	* Run `dotnet run build.cs` -- this builds TinyFFR.Native and can be repeated whenever you make changes
* Build solution/C# projects with `dotnet` or your IDE

## Linux

* Install .NET 10
* Install IDE(s) of your choice, Rider + CLion recommended
* Install required dependencies (alter the following commands if necessary according to your pre-existing environment):
	* `sudo apt-get update`
    * `sudo apt-get install -y cmake build-essential libglu1-mesa-dev ninja-build libc++-dev libc++abi-dev clang mesa-common-dev libxi-dev libxxf86vm-dev libgl1-mesa-dev libgles2-mesa-dev libegl1-mesa-dev libwayland-dev libxkbcommon-dev wayland-protocols`
* Open terminal in `/ThirdParty/` folder
	* Run `dotnet run build_and_copy_all_third_party.cs`; takes 30mins to an hour
* Open terminal in `/TinyFFR.Native/build/` folder
	* Run `dotnet run build.cs` -- this builds TinyFFR.Native and can be repeated whenever you make changes
* Build solution/C# projects with `dotnet` or your IDE

## Updating Third-Party Dependencies

Currently, I update third-party dependencies via the following steps:

* Clone the third-party repository in to a separate directory if you haven't already
* Checkout the specific tag/ref in that repo that you want to incorporate in to TinyFFR
* Create a new branch in TinyFFR (e.g. `git checkout -b update/assimp/v6.0.2` if you were updating to assimp v6.0.2) 
* Delete everything from the appropriate TinyFFR subdirectory (e.g. `/ThirdParty/assimp` for assimp), and then copy over the updated code from the standalone cloned repo to this subdirectory
* Commit everything with a useful message (e.g. "Update assimp to v6.0.2")
* Checkout `main` again (or whichever TinyFFR branch you want to merge the update in to)
* Merge the update branch (e.g. `git merge update/assimp/v6.0.2`) -- for simple updates this should be painless, but you will need to resolve merges where TinyFFR has modified the third-party code to keep TinyFFR's changes at the same time as updating

# Repository Structure

## /

The root folder contains the `Directory.Build.props` file which sets some universal constants used by every project in the solution.

## /TinyFFR/

This folder contains the primary C# library source.

## /TinyFFR.Native/

This folder contains the first-party native (C++) code bundled alongside the C# library.

## /ThirdParty/

This folder contains some third-party dependencies that TinyFFR relies on; each constituent folder is integrated as a [git subtree](https://www.atlassian.com/git/tutorials/git-subtree).

See above for build instructions.

## /Integrations/

This folder contains projects dedicated to providing integrations of TinyFFR with various third-party libraries/platforms.

## /Testing/

This folder contains various projects dedicated to testing TinyFFR.

### /Testing/LocalDevTesting/

This folder contains the _LocalDevTesting_ C# project. This project is designed for impromptu/ad-hoc testing of the library while developing it. 

* The **TestSetup** folder contains some scaffolding that sets up common test functionality.
* The **TestMain.cs** file is where you should write any test code you want to write (and has some instructions on usage).

### /Testing/ManualIntegrationTestRunner/

This folder contains a small project designed to run integration tests from **TinyFFR.Tests**. Because Cocoa on MacOS enforces that only the main thread can interact with UI attempting to run these tests from the test project directly tends to fail.

### /Testing/NupkgTesting/

This folder contains projects specifically for verifying/testing a correct + complete NuGet .nupkg build just before publishing.

Each project looks for NuGet packages in the usual sources (e.g. nuget.org) as well as in the **local_packages** folder.

Additionally, the C# project imports the files from **/Testing/TestCommon/** as a reference under the virtual folder `TestCommonRef`. These are imported as file references rather than a project reference as the `TestCommon` project itself references the primary TinyFFR project (and the whole purpose of `NupkgTesting` is to import TinyFFR via NuGet package instead). Any edits to these files from within this project will change the files in **/Testing/TestCommon/** (and therefore alter the common test functions for all test projects).

### /Testing/test_assets/

This folder contains some rendering assets (textures, models, etc.) that are used by all tests.

### /Testing/TestCommon/

This project defines some common functionality used by all tests (e.g. location of test assets, setup of native dependency resolution).

### /Testing/TinyFFR.Tests/

This folder contains all of the unit and integration tests for TinyFFR.

### /Testing/Integrations/

These folders contain specialized test projects for testing integrations with third-party libraries/platforms.

## /Documentation/

This folder contains the source for [the online manual](https://tinyffr.dev). The documentation is built using [Material for MKDocs](https://squidfunk.github.io/mkdocs-material/).

* The **source** folder contains the markdown files that ultimately become the pages of the documentation online.
* The **includes** and **theme_overrides** folders include meta-files for building the site.
* The output static HTML/CSS/JS will be built in to **output**. 

## /Publishing/

This folder contains projects and source dedicated to publishing TinyFFR (e.g. creation of NuGet packages).

### /Publishing/TinyFFR.NuGet/

This project builds the [.nupkg that is distributed on nuget.org](https://www.nuget.org/packages/Egodystonic.TinyFFR/).

* The **prebuilt_binaries** folder expects native dependencies built for the three supported platforms:
	* Windows binaries should be placed in **prebuilt_binaries/win-x64/**
	* Linux binaries should be placed in **prebuilt_binaries/linux-x64/**
	* MacOS binaries should be placed in **prebuilt_binaries/osx-arm64/**
* The final .nupkg is placed in the **build_output** folder.

### /Publishing/TinyFFR.**.NuGet/

The other projects in the `Publishing` folder help publish integration packages (e.g. packages integrating TinyFFR with third-party libraries/platforms).

They expect their corresponding integration projects (e.g. the corresponding project in /Integrations/) to be built first (in `Release` mode).

They also expect `TinyFFR.NuGet` to be built first.

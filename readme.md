# Nuke Examples Finder

## Descriptios

The NukeExamplesFinder searches public repositories on github, that contain a Nuke build script. 

## Configuration

The appsettings.json contains the following values:

* Credentials:GitHubToken - The [GitHub Token](https://github.com/settings/tokens) used by the GitHub client (needs public_repo scope)
* DataFiles:Path - The directory used to load and save the files (Repos.json, Directory.md)

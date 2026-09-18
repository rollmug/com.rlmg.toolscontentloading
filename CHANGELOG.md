# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.3.1]

### Changed

- all Samples' loaders' local file paths
- ExampleSequentialLoader no longer references UI displays

### Added

- HiddenStreamingAssets~ folders and supporting Editor scripts
- ExampleSequentialLoaderListener

### Removed

- streamingassets.unitypackage

## [0.3.0]

### Changed

- GraphQLLoader: Retry logic, fallback text asset for seeding missing config file
- CMSClientConfigData: Configuration for GraphQLLoader's new retry logic
- FileLoadingUtility: Support for fallback text asset parsing and seeding

### Added

- ContentLoaderStatusDisplay

### Removed

- EditorExample

## [0.2.1]

Filepath prefix bug fix and other features

### Changed

- ContentCacher
- ContentCacherAsync
- ContentLoader
- GraphQLLoader
- MediaLoadingUtility
- Caching Async Example/
- Local Media Example/

### Added

- FileLoadingUtility

## [0.2.0] - 2026-06-01

### Changed

- All Sample scenes' EventSystem Game Objects have been changed to support the new Input System

## [0.1.0] - 2026-05-29

### This is the first release of *\<RLMG Tools - Content Loading\>*.

First vertical slice of project, using the ContentLoading module as a test case.

### Added

- CMSClientConfigData
- ContentCacher
- ContentCacherAsync
- ContentLoader
- GraphQLLoader
- MediaLoadingUtility
- TextParsingUtility
- CSV/CSVLoader
- CSV/CSVMapper
- Directus/DirectusFile
- Common Example Assets/
- Caching Async Example/
- Config Example/
- CSV Example/
- GraphQL with Caching Content Example/
- Local Media Example/
- Remote Media with Caching Example/
- Sequential Loaders Example/


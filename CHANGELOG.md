# Roadmap

- [x] Support for custom url artifacts
  - [x] JSON Schema support
  - [x] Restore package from custom urls
  - [x] Tweaks to the package name validator
- [x] Renamed to _Client-Side Library Installer_
- [ ] Update package
  - [ ] Light bulb in `packman.json`
  - [ ] Visual indication that packages has update
- [ ] Uninstall package
  - [ ] Light bulb in `packman.json`
- [ ] Visual indication that folder/file is from package
- [ ] Cross-platform command line interface
- [ ] List packages under the _Dependencies_ node in Solution Explorer

Features that have a checkmark are complete and available for
download in the
[nightly build](http://vsixgallery.com/extension/ce753d0f-f511-4b2b-93de-5cc50145dca6/).

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 1.0
**2016-02-04**

- [x] Install packages
- [x] Restore packages
  - [x] Context menu button in Solution Explorer
  - [x] When saving `packman.json`
- [x] UI for package search
  - [x] Preview of what's being installed
  - [x] Select exactly which files to install
  - [x] Optional creation of package folders
  - [x] Optional saving to `packman.json`
  - [x] Preselect latest stable version
- [x] JSON manifest Intellisense
  - [x] JSON Schema
  - [x] Package name Intellisense
  - [x] Package version Intellisense
  - [x] Package file array Intellisense
- [x] JSON manifest validation
  - [x] JSON Schema
  - [x] Package version validation
  - [x] Package name validation
  - [x] Package files validation
- [x] Offline support (works from cache)
- [x] Options dialog

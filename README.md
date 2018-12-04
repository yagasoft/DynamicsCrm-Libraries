# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 1.2.4
---

A collection of libraries for common and extended operations in Dynamics CRM development that gives power to the developer and saves time.

### Features

  + Massive JS and CS libraries of common and generic functions and classes.
  + An extension to the out-of-the-box IOrganizationService. Supports:
    + Async operations with dependency
    + Transactions and rollback
    + Caching
    + Automatic pool/queue handling
    + Automatic thread handling
  + Dynamics-CRM-specific code analysis rules

### Guide

  + Download
    + Common (either)
      + NuGet: called "LinkDev.Libraries.Common"
      + Common.cs: in the repository
    + EnhancedOrgService
      + NuGet: called "LinkDev.Libraries.EnhancedOrgService"

### Dependencies

  + NuGet executable
    + Required to deploy the NuGet packages
    + Should be added to the 'lib' folder

## Changes

#### _v1.2.4 (2018-12-04)_
+ Fixed: removed redundant JS file
#### _v1.2.3 (2018-11-27)_
+ Changed: Common.cs file NuGet package
#### _v1.2.2 (2018-11-27)_
+ Fixed: spelling mistake
#### _v1.2.1 (2018-09-27)_
+ Added: conditions in FetchXML parser

---
**Copyright &copy; by Ahmed el-Sawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_

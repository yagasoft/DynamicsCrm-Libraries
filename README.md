# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 2.1.2
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
      + NuGet: called "Yagasoft.Libraries.Common"
      + Common.cs: in the repository
    + EnhancedOrgService
      + NuGet: called "Yagasoft.Libraries.EnhancedOrgService"

### Dependencies

  + NuGet executable
    + Required to deploy the NuGet packages
    + Should be added to the 'lib' folder

## Changes

#### _v2.1.2 (2019-03-04)_
+ Fixed: moved exception tracing for steps outside of a condition
+ Fixed: library references
#### _v2.1.1 (2019-02-27)_
+ Changed: moved to a new namespace
#### _v1.3.4 (2018-12-19)_
+ Added: relationship drill-through in placeholders
#### _v1.3.2 (2018-12-18)_
+ Changed: reworked the placeholder system
#### _v1.2.4 (2018-12-04)_
+ Fixed: removed redundant JS file
#### _v1.2.3 (2018-11-27)_
+ Changed: Common.cs file NuGet package
#### _v1.2.2 (2018-11-27)_
+ Fixed: spelling mistake
#### _v1.2.1 (2018-09-27)_
+ Added: conditions in FetchXML parser

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_

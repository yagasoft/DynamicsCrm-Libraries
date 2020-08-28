# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 2.4.1
---

A collection of libraries for common and extended operations in Dynamics CRM development that gives power to the developer and saves time.

## Features

  + Massive JS and CS libraries of common and generic functions and classes.
  + An extension to the out-of-the-box IOrganizationService. Supports:
    + Async operations with dependency
    + Transactions and rollback
    + Caching
    + Automatic pool/queue handling
    + Automatic thread handling
    + Connection warmup to improve caching performance
  + Dynamics-CRM-specific code analysis rules

## Guide

  + Download
    + Common (either)
      + Assembly: [Yagasoft.Libraries.Common](https://www.nuget.org/packages/Yagasoft.Libraries.Common)
      + Single CS file: [Yagasoft.Libraries.Common.File](https://www.nuget.org/packages/Yagasoft.Libraries.Common.File)
    + EnhancedOrgService
      + NuGet: [Yagasoft.Libraries.EnhancedOrgService](https://www.nuget.org/packages/Yagasoft.Libraries.EnhancedOrgService)

## Dependencies

  + NuGet executable
    + Required to deploy the NuGet packages
    + Should be added to the 'lib' folder

## Changes

#### _v2.4.1 (2020-08-28)_
+ Added: Pools accept a function to define custom logic for IOrganizationService creation
+ Added: warmup logic for CRM connections to improve caching performance
#### _v2.3.2 (2020-08-24)_
+ Changed: downgraded required CRM SDK version
+ Fixed: issues
#### _v2.3.1 (2020-08-10)_
+ Added: deferred execution feature of organisation requests
+ Added: pool dequeue timeout option
+ Added: a few helpers (CRM, error ... etc.)
+ Added: CRM Plugin Tracing service log feature
+ Added: timeout to Blocking Queue
+ Improved: connection error handling and message details
+ Improved: service parameters definition
+ Changed: supported SDK to v9.1.0.26 for .NET Framework 4.6.2
+ Fixed: ensure token is auto-refreshed internally as well
+ Fixed: BPF helper issues
+ Fixed: issues
#### _v2.2.3 (2019-10-06)_
+ Fixed: missing SecurityToken should bypass reauthentication
#### _v2.2.2 (2019-10-06)_
+ Added: token auto-refresh
#### _v2.2.1 (2019-10-05)_
+ Added: methods to remove entity from cache
#### _v2.1.3 (2019-03-16)_
+ Fixed: string modifier 'sub' throwing exception
#### _v2.1.2 (2019-03-04)_
+ Fixed: moved exception tracing for steps outside of a condition
+ Fixed: library references
#### _v2.1.1 (2019-02-27)_
+ Changed: moved to a new namespace

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](http://yagasoft.com))** -- _GPL v3 Licence_

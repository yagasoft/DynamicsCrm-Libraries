# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 3.1.1
---

A collection of libraries for common and extended operations in Dynamics CRM development that gives power to the developer and saves time.

## Features

  + Massive JS and CS libraries of common and generic functions and classes
  + An extension to the out-of-the-box IOrganizationService
    + Automatic pool/queue handling
    + Automatic thread handling
    + Connection warmup to improve caching performance
    + Caching of operation results
    + Deferred operations to run in a transaction
      + Accumulate operations from across the application to be executed in one go
    + Planned execution to be sent to CRM for execution
      + Return values from mid-execution operations can be used in later operations within the same transaction
    + Async operations with support for async/await and dependency definitions
  + Dynamics-CRM-specific code analysis rules

## Guide

  + Download
    + Common library (either packages)
      + Assembly: [Yagasoft.Libraries.Common](https://www.nuget.org/packages/Yagasoft.Libraries.Common)
      + Single CS file: [Yagasoft.Libraries.Common.File](https://www.nuget.org/packages/Yagasoft.Libraries.Common.File)
    + Enhanced Organisation Service library
      + NuGet: [Yagasoft.Libraries.EnhancedOrgService](https://www.nuget.org/packages/Yagasoft.Libraries.EnhancedOrgService)

## Changes

#### _v3.1.1 (2020-09-14)_
+ Added: execution planning, where a plan is executed in CRM itself for performance and atomicity reasons
+ Added: more convenience methods
+ Changed: refactoring
#### _v2.5.1 (2020-08-31)_
+ Added: use CrmServiceClient's Clone method internally for faster pooling of connections if available (SDK limits it for CRM Online only for now)
+ Added: DotNet optimisation options
+ Fixed: issues
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

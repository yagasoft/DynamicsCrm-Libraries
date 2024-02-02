# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

---

A collection of libraries for common and extended operations in Dynamics CRM development that gives power to the developer and saves time.

## Features

### Common Library

  + Massive JS and CS libraries of common and generic functions and classes
    + [CRM Logger](https://github.com/yagasoft/DynamicsCrm-CrmLogger)
    + [CRM Text Parser](https://github.com/yagasoft/Dynamics365-CrmTextParser)

### Enhanced Organisation Service library

  + An extension to the out-of-the-box IOrganizationService
  + Automatic service pool handling (core feature)
  + Connection warmup to improve initialisation performance (optional)
  + Caching of operation results (optional)
  + Automatic retry of failed operations (optional)
  + Operation events and statistics
  + Load balancer algorithms for multi-node environments
  + In-memory transactions
  + Deferred operations to run in a transaction
    + Accumulate operations from across the application to be executed in one go
  + Planned execution to be sent to CRM for execution
    + Return values from mid-execution operations can be used in later operations within the same transaction

## Guide

Add the following to the `.csproj` file to be able to compile.
```xml
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
```

### Common library (either packages)

  + Assembly: [Yagasoft.Libraries.Common](https://www.nuget.org/packages/Yagasoft.Libraries.Common)
  + Single CS file: [Yagasoft.Libraries.Common.File](https://www.nuget.org/packages/Yagasoft.Libraries.Common.File)

### Enhanced Organisation Service library

  + NuGet: [Yagasoft.Libraries.EnhancedOrgService](https://www.nuget.org/packages/Yagasoft.Libraries.EnhancedOrgService)
  + Guide: [EnhancedOrgService – Enterprise-grade CrmServiceClient | SwissKnife Series](https://blog.yagasoft.com/2021/05/enhancedorgservice-enterprise-grade-crmserviceclient-swissknife-series)

## Changes
+ Check Releases page for the later changes
#### _v1.1.1 (2015-05-15)_
+ Initial release

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](https://yagasoft.com))** -- _GPL v3 Licence_

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
#### _v2 to v8.1.2 (since 2019-02-27)_
+ Added: use CrmServiceClient's Clone method internally for faster pooling of connections if available (SDK limits it for CRM Online only for now)
+ Added: DotNet optimisation options
+ Added: execution planning, where a plan is executed in CRM itself for performance and atomicity reasons
+ Added: more convenience methods
+ Added: [EnhancedOrgService] CacheItemPriority to cache settings
+ Added: [EnhancedOrgService] node load balancer
+ Added: [EnhancedOrgService] self-balancing service
+ Added: [EnhancedOrgService] auto-retry mechanism
+ Added: [EnhancedOrgService] auto-retry failure events
+ Added: [EnhancedOrgService] operation-specific options
+ Added: [EnhancedOrgService] operation status events
+ Added: [EnhancedOrgService] operation statistics on all levels: service, pool, and factory
+ Added: [EnhancedOrgService] operation history to the service
+ Added: [EnhancedOrgService] deferred support for SDK methods (in addition to the custom ones that return a 'token')
+ Added: [EnhancedOrgService] custom cache factory parameter
+ Added: [EnhancedOrgService] exposed AutoSetMaxPerformanceParams through the pool 'helper' class
+ Added: [EnhancedOrgService] option to control service internal pool warm up
+ Added: [EnhancedOrgService] connection timeout parameter
+ Added: proper warm up logic for all pooling levels
+ Added: default factory and pool implementations for easier connection kick off
+ Added: CRM Text Parser
+ [Common] Added: features to the CRM Parser
+ [EnhancedOrgService] Added: RetrieveMultiple FetchXML convenience method
+ [Common] Improved: reworked the CRM Logger to be leaner and only concerned with CRM Plugins. Use NLog for everything else
+ Improved: [EnhancedOrgService] pool helpers
+ Improved: [EnhancedOrgService] generics and refactored
+ Improved: [EnhancedOrgService] optimised the interfaces and refactored
+ Improved: [EnhancedOrgService] internal calls by switching them to pass through service features
+ Improved: [Common] performance and memory
+ Improved: [Common] performance and memory
+ Improved: reworked the implicit pooling implementation for easier usage and more powerful features
+ Improved: reworked the self-balancing implementation for better code and maintenance
+ Improved: made the routing service more generic
+ Improved: reworked the interfaces to make more sense and for better maintenance (it breaks backward compatibility but the library hasn't been downloaded yet)
+ Improved: helpers for easier usage
+ Improved: updated rich editor
+ Changed: [EnhancedOrgService] tighten the service validity check to avoid internal operations triggering after Dispose; user must wait for all operations to finish
+ Fixed: [Common] RequireFormat helper
+ [EnhancedOrgService] Fixed: race condition
+ Fixed: [EnhancedOrgService] absolute expiration returns an absolute fixed date in the cache factory, now returns an absolute date from the time of call to the factory's 'get'
+ Fixed: [EnhancedOrgService] connection errors causing deadlocks
+ Fixed: [Common] caching issues
+ Fixed: [EnhancedOrgService] params default values
+ Fixed: [EnhancedOrgService] RetrieveMultiple helper not respecting the limit given
+ Fixed: [EnhancedOrgService] clear operation events when operation has finished to prevent memory leak
+ Fixed: [EnhancedOrgService] made parameters in generic retrieve optional
+ Fixed: [EnhancedOrgService] state validation for self-enqueuing/balancing service
+ Fixed: [EnhancedOrgService] threading issues
+ Fixed: [Common] log issues
+ Fixed: NuGet package requirements
+ Fixed: [Common] Params logger throwing an error
+ Fixed: internal pool initialisation
+ Fixed: timeout configuration
+ Fixed: disposal state
+ [Common] Fixed: ReplaceGroups issue
+ Removed: [EnhancedOrgService] async operations (use Task.Run or similar methods from .NET instead)
#### _v1.3.4 (2018-07-27)_
+ Initial release on GitHub
#### _v1.1.1 (2015-05-15)_
+ Initial release

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](https://yagasoft.com))** -- _GPL v3 Licence_

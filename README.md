# DynamicsCrm-Libraries

[![Join the chat at https://gitter.im/yagasoft/DynamicsCrm-Libraries](https://badges.gitter.im/yagasoft/DynamicsCrm-Libraries.svg)](https://gitter.im/yagasoft/DynamicsCrm-Libraries?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

### Version: 8.1.1
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

### Common library (either packages)

  + Assembly: [Yagasoft.Libraries.Common](https://www.nuget.org/packages/Yagasoft.Libraries.Common)
  + Single CS file: [Yagasoft.Libraries.Common.File](https://www.nuget.org/packages/Yagasoft.Libraries.Common.File)

### Enhanced Organisation Service library

  + NuGet: [Yagasoft.Libraries.EnhancedOrgService](https://www.nuget.org/packages/Yagasoft.Libraries.EnhancedOrgService)
  + Guide: [EnhancedOrgService – Enterprise-grade CrmServiceClient | SwissKnife Series](https://blog.yagasoft.com/2021/05/enhancedorgservice-enterprise-grade-crmserviceclient-swissknife-series)

## Changes

#### _v8.2.1 (2021-11-20)_
+ [Common] Improved: reworked the CRM Logger to be leaner and only concerned with CRM Plugins. Use NLog for everything else.
#### _v7.2.1 (2021-10-01)_
+ [Common] Added: features to the CRM Parser
+ [Common] Fixed: ReplaceGroups issue
#### _v7.1.3 (2021-09-20)_
+ Fixed: [Common] Params logger throwing an error
#### _v7.1.2 (2021-09-17)_
+ Added: CRM Text Parser
+ Improved: updated rich editor
+ Fixed: issues
#### _v6.1.2 (2021-05-25)_
+ Fixed: NuGet package requirements
#### _v6.1.1 (2021-05-08)_
+ Added: default factory and pool implementations for easier connection kick off
+ Improved: reworked the implicit pooling implementation for easier usage and more powerful features
+ Improved: reworked the self-balancing implementation for better code and maintenance
+ Improved: made the routing service more generic
+ Improved: reworked the interfaces to make more sense and for better maintenance (it breaks backward compatibility but the library hasn't been downloaded yet)
+ Improved: helpers for easier usage
#### _v5.3.3 (2021-05-06)_
+ Added: proper warm up logic for all pooling levels
+ Fixed: internal pool initialisation
+ Fixed: timeout configuration
+ Fixed: disposal state
#### _v5.3.2 (2021-04-29)_
+ Added: [EnhancedOrgService] option to control service internal pool warm up
+ Improved: [Common] performance and memory
+ Fixed: [EnhancedOrgService] RetrieveMultiple helper not respecting the limit given
+ Fixed: [Common] issues
#### _v5.2.1 (2021-01-02)_
+ Added: [EnhancedOrgService] connection timeout parameter
#### _v5.1.4 (2020-12-27)_
+ Improved: [Common] performance and memory
+ Fixed: [EnhancedOrgService] clear operation events when operation has finished to prevent memory leak
+ Fixed: [EnhancedOrgService] made parameters in generic retrieve optional
+ Fixed: [Common] issues
#### _v5.1.3 (2020-11-22)_
+ Fixed: [EnhancedOrgService] state validation for self-enqueuing/balancing service
+ Fixed: [EnhancedOrgService] threading issues
+ Fixed: [Common] caching issues
#### _v5.1.2 (2020-11-11)_
+ Fixed: [Common] log issues
#### _v5.1.1 (2020-11-07)_
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
+ Improved: [EnhancedOrgService] optimised the interfaces and refactored
+ Improved: [EnhancedOrgService] internal calls by switching them to pass through service features
+ Changed: [EnhancedOrgService] tighten the service validity check to avoid internal operations triggering after Dispose; user must wait for all operations to finish
+ Fixed: [Common] RequireFormat helper
+ Fixed: [EnhancedOrgService] params default values
+ Removed: [EnhancedOrgService] async operations (use Task.Run or similar methods from .NET instead)
#### _v4.1.1 (2020-10-02)_
+ Added: [EnhancedOrgService] CacheItemPriority to cache settings
+ Improved: [EnhancedOrgService] pool helpers
+ Improved: [EnhancedOrgService] generics and refactored
+ Fixed: [EnhancedOrgService] absolute expiration returns an absolute fixed date in the cache factory, now returns an absolute date from the time of call to the factory's 'get'
+ Fixed: [EnhancedOrgService] connection errors causing deadlocks
+ Fixed: [Common] caching issues
#### _v3.1.1 (2020-09-14)_
+ Added: execution planning, where a plan is executed in CRM itself for performance and atomicity reasons
+ Added: more convenience methods
+ Changed: refactoring
#### _v2.5.1 (2020-08-31)_
+ Added: use CrmServiceClient's Clone method internally for faster pooling of connections if available (SDK limits it for CRM Online only for now)
+ Added: DotNet optimisation options
+ Fixed: issues
#### _v2.1.1 (2019-02-27)_
+ Changed: moved to a new namespace

---
**Copyright &copy; by Ahmed Elsawalhy ([Yagasoft](https://yagasoft.com))** -- _GPL v3 Licence_

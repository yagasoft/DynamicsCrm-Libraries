#region Imports

using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;
using Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Operations
{
	public interface IOperationStats
	{
		event EventHandler<IOrganizationService, OperationStatusEventArgs, IOrganizationService> OperationStatusChanged;

		/// <summary>
		///     Total number of core operations performed by this service.
		/// </summary>
		int RequestCount { get; }

		/// <summary>
		///     Count of core operations that failed in this service since its creation.<br />
		///     If this service is pooled, it will still keep this value even when disposed (services are recycled).
		/// </summary>
		int FailureCount { get; }

		/// <summary>
		///     Number of failures out of the total operations.
		/// </summary>
		double FailureRate { get; }

		/// <summary>
		///     Count of retries performed by this service.
		/// </summary>
		int RetryCount { get; }

		IEnumerable<Operation> PendingOperations { get; }

		/// <summary>
		///     The history of operations executed by this service object.<br />
		///     Includes the <see cref="OrganizationRequest" /> and <see cref="OrganizationResponse" /> objects,
		///     the order of the request (index), and time taken to execute it<br />
		///     <see cref="Operation.UndoRequest" /> is filled if transactions were enabled during initialisation of this
		///     service's factory.
		/// </summary>
		IEnumerable<Operation> ExecutedOperations { get; }
	}
}

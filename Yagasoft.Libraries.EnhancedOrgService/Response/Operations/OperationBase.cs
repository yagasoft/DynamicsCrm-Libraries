#region Imports

using System;
using Microsoft.Xrm.Sdk;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public enum OperationStatus
	{
		Success,
		Failure,
		Pending,
		Ready
	}

	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public abstract class OperationBase
	{
		public readonly OrganizationRequest Request;
		public OrganizationRequest UndoRequest { get; internal set; }

		private Exception exception;
		private DateTime endDate;

		/// <summary>
		///     Exception thrown by the CRM Organisation Service.
		/// </summary>
		/// <value>
		///     The exception.
		/// </value>
		public Exception Exception
		{
			get => exception;

			internal set
			{
				exception = value;

				EndDate = DateTime.Now;

				OperationStatus = exception == null ? OperationStatus.Ready : OperationStatus.Failure;
			}
		}

		public DateTime StartDate { get; protected set; }
		public TimeSpan TotalTime { get; protected set; }

		public DateTime EndDate
		{
			get => endDate;
			protected set
			{
				endDate = value;
				TotalTime = endDate - StartDate;
			}
		}

		public OperationStatus OperationStatus { get; internal set; }

		/// <summary>
		///     Index of the operation since the creation of the enhanced service that is handling this operation.
		/// </summary>
		/// <value>
		///     The index.
		/// </value>
		public int Index { get; internal set; }

		protected OperationBase(OrganizationRequest request = null, OrganizationRequest undoRequest = null)
		{
			StartDate = DateTime.Now;
			OperationStatus = OperationStatus.Ready;

			Request = request;
			UndoRequest = undoRequest;
		}
	}
}

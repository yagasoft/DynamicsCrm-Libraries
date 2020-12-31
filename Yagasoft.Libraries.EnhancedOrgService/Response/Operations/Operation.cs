#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Events;
using Yagasoft.Libraries.EnhancedOrgService.Events.EventArgs;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Response.Operations
{
	public enum Status
	{
		Success,
		Failure,
		InProgress,
		Retry,
		Ready
	}

	/// <summary>
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public class Operation
	{
		public OrganizationRequest Request { get; internal set; }

		public OrganizationRequest UndoRequest { get; set; }

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
				OperationStatus = exception == null ? Status.Success : Status.Failure;
			}
		}

		public DateTime? StartDate { get; protected set; }
		public DateTime? EndDate { get; protected set; }
		public TimeSpan? TotalTime => EndDate - StartDate;

		public Status? OperationStatus
		{
			get => operationStatus;
			internal set
			{
				switch (value)
				{
					case Status.InProgress when StartDate == null:
						StartDate = DateTime.Now;
						break;

					case Status.Success when EndDate == null:
					case Status.Failure when EndDate == null:
						EndDate = DateTime.Now;
						break;
				}

				operationStatus = value;
				OnOperationStatusChanged(new OperationStatusEventArgs(this, OperationStatus));
			}
		}

		/// <summary>
		///     Index of the operation since the creation of the enhanced service that is handling this operation.
		/// </summary>
		/// <value>
		///     The index.
		/// </value>
		public int Index { get; internal set; }

		/// <summary>
		///     Gets or sets the response. 'Get' blocks until the response is ready.
		/// </summary>
		/// <value>
		///     The response.
		/// </value>
		/// <exception cref="System.Exception">Can't set the response of a failed response: Exception.Message</exception>
		public OrganizationResponse Response
		{
			get
			{
				if (Exception != null)
				{
					throw Exception;
				}

				return response;
			}
			internal set
			{
				if (Exception != null)
				{
					throw new Exception("Can't set the response of a failed response: " + Exception.Message);
				}

				// set the response value
				response = value;

				OperationStatus = Status.Success;
			}
		}

		internal event EventHandler<Operation, OperationStatusEventArgs> OperationStatusChanged;

		private Exception exception;
		private OrganizationResponse response;
		private Status? operationStatus;

		internal Operation(OrganizationRequest request = null, OrganizationRequest undoRequest = null)
		{
			Request = request;
			UndoRequest = undoRequest;
		}
		
		internal void ClearEventHandlers()  
		{  
			OperationStatusChanged = null;  
		}
		
		private void OnOperationStatusChanged(OperationStatusEventArgs e)  
		{  
			OperationStatusChanged?.Invoke(this, e);  
		}
	}

	public class Operation<TResponse> : Operation where TResponse : OrganizationResponse
	{
		/// <inheritdoc cref="Operation.Response"/>
		public new TResponse Response => base.Response as TResponse;

		internal Operation(OrganizationRequest request = null, OrganizationRequest undoRequest = null)
		{
			Request = request;
			UndoRequest = undoRequest;
		}
	}
}

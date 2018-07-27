#region Imports

using System;
using Microsoft.Xrm.Sdk;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Response
{
	/// <summary>
	///     Author: Ahmed el-Sawalhy
	/// </summary>
	public class Operation<TResponseType> : OperationBase
	{
		private TResponseType response;

		/// <summary>
		/// Gets or sets the response. 'Get' blocks until the response is ready.
		/// </summary>
		/// <value>
		/// The response.
		/// </value>
		/// <exception cref="System.Exception">Can't set the response of a failed response: Exception.Message</exception>
		public TResponseType Response
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

				OperationStatus = response == null ? OperationStatus.Ready : OperationStatus.Success;

				EndDate = DateTime.Now;
			}
		}

		public Operation(OrganizationRequest request = null, OrganizationRequest undoRequest = null)
			: base(request, undoRequest)
		{
		}

	}
}

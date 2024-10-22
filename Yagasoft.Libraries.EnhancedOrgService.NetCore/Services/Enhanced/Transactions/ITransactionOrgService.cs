#region Imports

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Transactions;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Services.Enhanced.Transactions
{
	public interface ITransactionOrgService
	{
		/// <summary>
		///     Starts a new transaction. After calling this method, all service requests can be undone by calling
		///     <see cref="EnhancedOrgServiceBase.UndoTransaction" />.
		/// </summary>
		/// <param name="transactionId">[OPTIONAL] The transaction ID.</param>
		/// <returns>A new transaction object to use when reverting the transaction.</returns>
		Transaction BeginTransaction(string transactionId = null);

		/// <summary>
		///     Reverts the transaction, which executes 'undo' requests for every service request made since the start of this
		///     transaction.
		///     If no transaction is given, it reverts ALL transactions.
		/// </summary>
		/// <param name="transaction">[OPTIONAL] The transaction to revert.</param>
		void UndoTransaction(Transaction transaction = null);

		/// <summary>
		///     Adds undo logic for the request type given to the cache.
		/// </summary>
		/// <typeparam name="TRequestType">The type of the request to undo.</typeparam>
		/// <param name="undoFunction">The undo function, which takes the original request and returns the undo request.</param>
		void AddUndoLogicToCache<TRequestType>(
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction)
			where TRequestType : OrganizationRequest;

		/// <summary>
		///     Ends the transaction, which excludes requests from future reverts; however, nested transactions can still be
		///     reverted!
		/// </summary>
		/// <param name="transaction">The transaction to end.</param>
		void EndTransaction(Transaction transaction = null);
	}
}

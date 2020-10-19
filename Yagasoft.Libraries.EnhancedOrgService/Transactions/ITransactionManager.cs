#region Imports

using System;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Transactions
{
	public interface ITransactionManager
	{
		bool IsTransactionInEffect();

		Transaction BeginTransaction(string transactionId = null, Operation startingPoint = null);

		void ProcessRequest(IOrganizationService service, Operation operation,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction = null);

		void UndoTransaction(IOrganizationService service, Transaction transaction = null);

		void EndTransaction(Transaction transaction = null);
	}
}

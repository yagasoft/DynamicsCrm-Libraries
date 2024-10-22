﻿#region Imports

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Transactions
{
	/// <summary>
	///     Author: Ahmed Elsawalhy (Yagasoft)
	/// </summary>
	internal class TransactionManager : ITransactionManager
	{
		private readonly Stack<Operation> operationsStack = new();
		private readonly Stack<Transaction> transactionsStack = new();

		public bool IsTransactionInEffect()
		{
			return transactionsStack.Any();
		}

		public Transaction BeginTransaction(string transactionId = null,
			Operation startingPoint = null)
		{
			if (transactionsStack.Any(transactionQ => transactionQ.Id == transactionId))
			{
				throw new Exception("Transaction with same ID already exists!");
			}

			if (string.IsNullOrEmpty(transactionId))
			{
				transactionId = Guid.NewGuid().ToString();
			}

			var transaction = new Transaction(transactionId, startingPoint);
			transactionsStack.Push(transaction);

			return transaction;
		}

		public void ProcessRequest(IOrganizationService service, Operation operation,
			Func<IOrganizationService, OrganizationRequest, OrganizationRequest> undoFunction = null)
		{
			if (!IsTransactionInEffect())
			{
				throw new Exception("No transaction in effect!");
			}

			try
			{
				// get request from operation
				var request = operation.Request;

				// get the undo request corresponding to the given request
				operation.UndoRequest = undoFunction == null
					? UndoHelper.GenerateReverseRequest(service, request)
					: undoFunction(service, request);

				// register this response as the starting point of the latest transaction
				transactionsStack.Peek().StartingPoint ??= operation;
				operationsStack.Push(operation);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to create undo request for the given request! => " + ex.Message, ex);
			}
		}

		public void UndoTransaction(IOrganizationService service, Transaction transaction = null)
		{
			if (transaction != null && !transactionsStack.Contains(transaction))
			{
				throw new Exception("Transaction does not exist!");
			}

			try
			{
				Transaction currentTransaction;

				do
				{
					currentTransaction = transactionsStack.Pop();
					currentTransaction.Current = false;

					// skip if the transaction is empty
					if (currentTransaction.StartingPoint == null)
					{
						continue;
					}

					if (operationsStack.All(undoRequestQ => currentTransaction.StartingPoint != undoRequestQ))
					{
						throw new Exception("Check-point does not exist!");
					}

					Operation operation;

					do
					{
						operation = operationsStack.Pop();
						//operation.IsDoneWait();		// TODO: wait for operation to finish first
						service.Execute(operation.UndoRequest); // undo
					}
					while (operationsStack.Any() && currentTransaction.StartingPoint != operation);
				}
				while (currentTransaction != transaction && transaction != null);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to undo transaction(s)! => " + ex.Message, ex);
			}
		}

		public void EndTransaction(Transaction transaction = null)
		{
			if (transaction != null && !transactionsStack.Contains(transaction))
			{
				throw new Exception("Transaction does not exist!");
			}

			Transaction currentTransaction;

			do
			{
				currentTransaction = transactionsStack.Pop();
				currentTransaction.Current = false;
			}
			while (currentTransaction != transaction && transaction != null);
		}
	}
}

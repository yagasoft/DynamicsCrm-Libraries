#region Imports

using System;
using System.Linq;
using Yagasoft.Libraries.Common;
using Yagasoft.Libraries.EnhancedOrgService.Exceptions;
using Yagasoft.Libraries.EnhancedOrgService.Helpers;
using Yagasoft.Libraries.EnhancedOrgService.Params;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Builders
{
	/// <summary>
	///     Assists in building a template for Enhanced Organisation Services.
	///     To get a new builder object, use the property <see cref="NewBuilder" />.<br />
	///     You have to <see cref="Initialise" />, add features, and then <see cref="Finalise" />.<br />
	///     Author: Ahmed Elsawalhy
	/// </summary>
	public sealed class EnhancedServiceBuilder : ProcessBase
	{
		private EnhancedServiceParams parameters;

		public static EnhancedServiceBuilder NewBuilder => new EnhancedServiceBuilder();

		private EnhancedServiceBuilder()
		{ }

		public EnhancedServiceBuilder Initialise(string connectionString)
		{
			ValidateInitialised(false);
			ValidateFinalised(false);

			if (connectionString.Trim(';').Split(';').SelectMany(e => e.Split('=')).Count() % 2 != 0)
			{
				throw new FormatException("Connection string format is incorrect.");
			}

			parameters = new EnhancedServiceParams(connectionString);
			IsInitialised = true;

			return this;
		}

		public EnhancedServiceBuilder AddCaching(CachingParams cachingParams = null)
		{
			ValidateInitialised();
			ValidateFinalised(false);

			parameters.IsCachingEnabled = true;
			parameters.CachingParams = cachingParams ?? new CachingParams();

			return this;
		}

		public EnhancedServiceBuilder AddTransactions(TransactionParams transactionParams = null)
		{
			ValidateInitialised();
			ValidateFinalised(false);

			parameters.IsTransactionsEnabled = true;
			parameters.TransactionParams = transactionParams ?? new TransactionParams();

			return this;
		}

		public EnhancedServiceBuilder AddConcurrency(ConcurrencyParams concurrencyParams = null)
		{
			ValidateInitialised();
			ValidateFinalised(false);

			parameters.IsConcurrencyEnabled = true;
			parameters.ConcurrencyParams = concurrencyParams ?? new ConcurrencyParams();

			return this;
		}

		/// <summary>
		///     Sets the application to wait for async operations in the service to finish before exiting.
		/// </summary>
		public EnhancedServiceBuilder HoldAppForAsync()
		{
			ValidateInitialised();
			ValidateFinalised(false);

			if (!parameters.IsConcurrencyEnabled)
			{
				throw new UnsupportedException("Concurrency is not enabled.");
			}

			parameters.ConcurrencyParams.IsAsyncAppHold = true;

			return this;
		}

		public EnhancedServiceBuilder Finalise()
		{
			ValidateInitialised();
			ValidateFinalised(false);

			if (parameters.ConnectionParams?.ConnectionString.IsEmpty() == true)
			{
				throw new ArgumentNullException(nameof(parameters.ConnectionParams.ConnectionString),
					"Connection String is missing in Enhanced Service parameters.");
			}

			parameters.IsLocked = true;
			IsFinalised = true;

			return this;
		}

		/// <summary>
		///     Returns the template to be used with the factory.
		/// </summary>
		public EnhancedServiceParams GetBuild()
		{
			ValidateFinalised();
			return parameters;
		}
	}
}

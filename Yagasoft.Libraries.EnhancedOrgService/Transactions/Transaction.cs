#region Imports

using Yagasoft.Libraries.EnhancedOrgService.Response.Operations;

#endregion

namespace Yagasoft.Libraries.EnhancedOrgService.Transactions
{
	/// <summary>
	///     Author: Ahmed Elsawalhy (Yagasoft)
	/// </summary>
	public class Transaction
	{
		public readonly string Id;

		/// <summary>
		///     If true, indicates that the transaction hasn't ended yet.
		/// </summary>
		public bool Current = true;

		internal Operation StartingPoint;

		internal Transaction(string id, Operation startingPoint = null)
		{
			Id = id;
			StartingPoint = startingPoint;
		}
	}
}

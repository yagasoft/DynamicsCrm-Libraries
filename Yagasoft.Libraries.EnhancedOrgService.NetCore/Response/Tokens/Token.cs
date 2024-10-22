namespace Yagasoft.Libraries.EnhancedOrgService.Response.Tokens
{
	public class Token<TValue> : IToken<TValue>
	{
		public TValue Value { get; internal set; }
	}
}

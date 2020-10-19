namespace Yagasoft.Libraries.EnhancedOrgService.State
{
	public interface IStateful
	{
		void ValidateState(bool isValid = true);
	}
}

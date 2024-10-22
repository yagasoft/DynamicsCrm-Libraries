namespace Yagasoft.Libraries.EnhancedOrgService.Events
{
	public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
}

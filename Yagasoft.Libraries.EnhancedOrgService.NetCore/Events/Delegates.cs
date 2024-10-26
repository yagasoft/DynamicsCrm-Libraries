namespace Yagasoft.Libraries.EnhancedOrgService.Events
{
	public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
	public delegate void EventHandler<in TSender, in TArgs, in TExtra>(TSender sender, TArgs args, TExtra extra);
}

namespace Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp
{
    public interface IWarmUp
    {
        /// <summary>
        ///     Starts creating connections to fill the internal queue. Makes retrieving the connections a lot faster later.
        /// </summary>
        void WarmUp();

        /// <summary>
        ///     Stops the warmup process.
        /// </summary>
        void EndWarmup();
    }
}

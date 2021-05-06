using System;
using System.Threading;

namespace Yagasoft.Libraries.EnhancedOrgService.Pools.WarmUp
{
    public class WarmUp
    {
        private readonly Action warmUpLogic;
        private Thread warmupThread;
        private bool isWarmUp;

        public WarmUp(Action warmUpLogic)
        {
            this.warmUpLogic = warmUpLogic;
        }

        public void Start()
        {
            lock (this)
            {
                isWarmUp = true;

                if (warmupThread?.IsAlive == true)
                {
                    return;
                }

                warmupThread =
                    new Thread(
                        () =>
                        {
                            var isFailed = false;

                            while (isWarmUp)
                            {
                                try
                                {
                                    warmUpLogic();
                                }
                                catch
                                {
                                    if (isFailed)
                                    {
                                        isWarmUp = false;
                                    }
                                    else
                                    {
                                        isFailed = true;
                                    }
                                }
                            }
                        }) { IsBackground = true };

                warmupThread.Start();
            }
        }

        public void End()
        {
            isWarmUp = false;
        }



    }
}

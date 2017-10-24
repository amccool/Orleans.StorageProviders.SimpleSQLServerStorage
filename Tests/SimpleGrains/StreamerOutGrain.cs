using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using SimpleGrainInterfaces;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SimpleGrains
{
    public class StreamerOutGrain : MyGrain, IStreamerOutGrain
    {
        private IAsyncStream<int> producer;
        private int numProducedItems;
        private IDisposable producerTimer;
        internal Logger logger;
        internal readonly static string RequestContextKey = "RequestContextField";
        internal readonly static string RequestContextValue = "JustAString";

        public override Task OnActivateAsync()
        {
            logger = base.GetLogger("StreamerOutGrain " + base.IdentityString);
            logger.Info("OnActivateAsync");
            numProducedItems = 0;
            return Task.CompletedTask;
        }


        public Task BecomeProducer(Guid streamId, string streamNamespace, string providerToUse)
        {
            logger.Info("BecomeProducer");
            IStreamProvider streamProvider = base.GetStreamProvider(providerToUse);
            producer = streamProvider.GetStream<int>(streamId, streamNamespace);
            return Task.CompletedTask;
        }

        public Task ClearNumberProduced()
        {
            numProducedItems = 0;
            return Task.CompletedTask;
        }

        public Task<int> GetNumberProduced()
        {
            logger.Info("GetNumberProduced {0}", numProducedItems);
            return Task.FromResult(numProducedItems);
        }

        public Task Produce()
        {
            return Fire();
        }

        public Task StartPeriodicProducing()
        {
            logger.Info("StartPeriodicProducing");
            producerTimer = base.RegisterTimer(TimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
            return Task.CompletedTask;
        }

        public Task StopPeriodicProducing()
        {
            logger.Info("StopPeriodicProducing");
            producerTimer.Dispose();
            producerTimer = null;
            return Task.CompletedTask;
        }

        private Task TimerCallback(object state)
        {
            return producerTimer != null ? Fire() : Task.CompletedTask;
        }

        private async Task Fire([CallerMemberName] string caller = null)
        {
            RequestContext.Set(RequestContextKey, RequestContextValue);
            await producer.OnNextAsync(numProducedItems);
            numProducedItems++;
            logger.Info("{0} (item={1})", caller, numProducedItems);
        }

        public override Task OnDeactivateAsync()
        {
            logger.Info("OnDeactivateAsync");
            return Task.CompletedTask;
        }

    }
}

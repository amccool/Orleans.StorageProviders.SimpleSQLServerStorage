using SimpleGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Streams;

namespace SimpleGrain
{
    public class StreamerInGrain : IStreamerInGrain
    {
        public Task<StreamSubscriptionHandle<int>> BecomeConsumer(Guid streamId, string streamNamespace, string providerToUse)
        {
            throw new NotImplementedException();
        }

        public Task ClearNumberConsumed()
        {
            throw new NotImplementedException();
        }

        public Task Deactivate()
        {
            throw new NotImplementedException();
        }

        public Task<IList<StreamSubscriptionHandle<int>>> GetAllSubscriptions(Guid streamId, string streamNamespace, string providerToUse)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<StreamSubscriptionHandle<int>, Tuple<int, int>>> GetNumberConsumed()
        {
            throw new NotImplementedException();
        }

        public Task<StreamSubscriptionHandle<int>> Resume(StreamSubscriptionHandle<int> handle)
        {
            throw new NotImplementedException();
        }

        public Task StopConsuming(StreamSubscriptionHandle<int> handle)
        {
            throw new NotImplementedException();
        }
    }
}

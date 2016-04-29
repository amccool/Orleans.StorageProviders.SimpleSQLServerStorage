using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrainInterfaces
{
    public interface IStreamerInGrain : IGrainWithGuidKey
    {
        Task<StreamSubscriptionHandle<int>> BecomeConsumer(Guid streamId, string streamNamespace, string providerToUse);

        Task<StreamSubscriptionHandle<int>> Resume(StreamSubscriptionHandle<int> handle);

        Task StopConsuming(StreamSubscriptionHandle<int> handle);

        Task<IList<StreamSubscriptionHandle<int>>> GetAllSubscriptions(Guid streamId, string streamNamespace, string providerToUse);

        Task<Dictionary<StreamSubscriptionHandle<int>, Tuple<int, int>>> GetNumberConsumed();

        Task ClearNumberConsumed();

        Task Deactivate();

    }
}

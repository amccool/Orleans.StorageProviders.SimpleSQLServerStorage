using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrainInterfaces
{
    public interface IStreamerOutGrain : IGrainWithGuidKey
    {
        Task BecomeProducer(Guid streamId, string streamNamespace, string providerToUse);

        Task StartPeriodicProducing();

        Task StopPeriodicProducing();

        Task<int> GetNumberProduced();

        Task ClearNumberProduced();
        Task Produce();

    }
}

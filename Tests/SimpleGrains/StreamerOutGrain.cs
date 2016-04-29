using SimpleGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrain
{
    public class StreamerOutGrain : IStreamerOutGrain
    {
        public Task BecomeProducer(Guid streamId, string streamNamespace, string providerToUse)
        {
            throw new NotImplementedException();
        }

        public Task ClearNumberProduced()
        {
            throw new NotImplementedException();
        }

        public Task<int> GetNumberProduced()
        {
            throw new NotImplementedException();
        }

        public Task Produce()
        {
            throw new NotImplementedException();
        }

        public Task StartPeriodicProducing()
        {
            throw new NotImplementedException();
        }

        public Task StopPeriodicProducing()
        {
            throw new NotImplementedException();
        }
    }
}

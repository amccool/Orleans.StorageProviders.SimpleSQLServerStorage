using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSQLServerStorage.Tests
{
    public abstract class TestClusterPerTest : OrleansTestingBase, IDisposable
    {
        static TestClusterPerTest()
        {
        }

        //protected TestCluster HostedCluster { get; private set; }
        protected TestCluster HostedCluster { get; set; }

        public TestClusterPerTest()
        {
            GrainClient.Uninitialize();
            var testCluster = CreateTestCluster();
            if (testCluster.Primary == null)
            {
                testCluster.Deploy();
            }
            this.HostedCluster = testCluster;
        }

        public virtual TestCluster CreateTestCluster()
        {
            var options = new TestClusterOptions();
            options.ExtendedFallbackOptions.TraceToConsole = true;
            return new TestCluster(options);
        }

        public virtual void Dispose()
        {
            this.HostedCluster.StopAllSilos();
        }
    }

}

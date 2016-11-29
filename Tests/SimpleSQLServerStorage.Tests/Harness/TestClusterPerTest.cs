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
            TestClusterOptions.DefaultTraceToConsole = true;
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
            return new TestCluster();
        }

        public virtual void Dispose()
        {
            this.HostedCluster.StopAllSilos();
        }
    }

}

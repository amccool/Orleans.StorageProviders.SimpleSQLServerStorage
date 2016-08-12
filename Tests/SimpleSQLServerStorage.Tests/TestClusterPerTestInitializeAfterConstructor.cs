using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSQLServerStorage.Tests
{
    public abstract class TestClusterPerTestInitializeAfterConstructor : OrleansTestingBase, IDisposable
    {
        static TestClusterPerTestInitializeAfterConstructor()
        {
            TestClusterOptions.DefaultTraceToConsole = true;
        }

        //protected TestCluster HostedCluster { get; private set; }
        protected TestCluster HostedCluster { get; set; }

        public TestClusterPerTestInitializeAfterConstructor()
        {
        }

        protected void Initialize()
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

using Orleans;
using Orleans.Serialization;
using Orleans.TestingHost;
using System;

namespace SimpleSQLServerStorage.Tests
{
    public abstract class BaseTestClusterFixture : IDisposable
    {
        static BaseTestClusterFixture()
        {
        }

        protected BaseTestClusterFixture()
        {
            GrainClient.Uninitialize();
            var testCluster = CreateTestCluster();
            if (testCluster.Primary == null)
            {
                testCluster.Deploy();
            }
            this.HostedCluster = testCluster;
        }

        protected abstract TestCluster CreateTestCluster();

        public TestCluster HostedCluster { get; private set; }

        public virtual void Dispose()
        {
            this.HostedCluster.StopAllSilos();
        }
    }

}
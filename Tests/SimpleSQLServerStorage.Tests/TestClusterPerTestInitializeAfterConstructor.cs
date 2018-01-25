using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime.Configuration;

namespace SimpleSQLServerStorage.Tests
{
    public abstract class TestClusterPerTestInitializeAfterConstructor : OrleansTestingBase, IDisposable
    {
        static TestClusterPerTestInitializeAfterConstructor()
        {
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
            TestClusterOptions options = new TestClusterOptions();
            options.ClusterConfiguration.ApplyToAllNodes(c => c.DefaultTraceLevel = Orleans.Runtime.Severity.Warning);
            options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceToConsole = false);
            options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceFileName = string.Empty);
            options.ClusterConfiguration.ApplyToAllNodes(c => c.TraceFilePattern = string.Empty);
            options.ClusterConfiguration.ApplyToAllNodes(c => c.StatisticsWriteLogStatisticsToTable = false);

            options.ClientConfiguration.DefaultTraceLevel = Orleans.Runtime.Severity.Warning;
            options.ClientConfiguration.TraceToConsole = false;

            return new TestCluster(options);
        }

        public virtual void Dispose()
        {
            this.HostedCluster.StopAllSilos();
        }
    }

}

using Orleans;
using Orleans.Runtime;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SimpleSQLServerStorage.Tests
{
    //public static class TestingClusterExtensions
    //{
    //    /// <summary>
    //    /// Restart the default Primary and Secondary silos.
    //    /// </summary>
    //    public static void RestartDefaultSilos(this TestCluster testCluster, bool pickNewDeploymentId = false)
    //    {
    //        TestingSiloOptions primarySiloOptions = testCluster.Primary != null ? testCluster.Primary.Options : null;
    //        TestingSiloOptions secondarySiloOptions = testCluster.Secondary != null ? testCluster.Secondary.Options : null;
    //        // Restart as the same deployment
    //        string deploymentId = testCluster.DeploymentId;

    //        testCluster.StopDefaultSilos();

    //        testCluster.DeploymentId = pickNewDeploymentId ? null : deploymentId;
    //        if (primarySiloOptions != null)
    //        {
    //            primarySiloOptions.PickNewDeploymentId = pickNewDeploymentId;
    //            testCluster.Primary = StartOrleansSilo(Silo.SiloType.Primary, primarySiloOptions, InstanceCounter++);
    //        }
    //        if (secondarySiloOptions != null)
    //        {
    //            secondarySiloOptions.PickNewDeploymentId = pickNewDeploymentId;
    //            testCluster.Secondary = StartOrleansSilo(Silo.SiloType.Secondary, secondarySiloOptions, InstanceCounter++);
    //        }

    //        testCluster.WaitForLivenessToStabilizeAsync().Wait();
    //        GrainClient.Initialize(testCluster.ClientConfig);
    //    }

    //}





    public abstract class OrleansTestingBase
    {
        protected static readonly Random random = new Random();

        public Logger logger
        {
            get { return GrainClient.Logger; }
        }

        protected static IGrainFactory GrainFactory { get { return GrainClient.GrainFactory; } }

        protected static long GetRandomGrainId()
        {
            return random.Next();
        }

        protected void TestSilosStarted(int expected)
        {
            IManagementGrain mgmtGrain = GrainClient.GrainFactory.GetGrain<IManagementGrain>(1); // is nornally RuntimeInterfaceConstants.SYSTEM_MANAGEMENT_ID

            Dictionary<SiloAddress, SiloStatus> statuses = mgmtGrain.GetHosts(onlyActive: true).Result;
            foreach (var pair in statuses)
            {
                logger.Info("       ######## Silo {0}, status: {1}", pair.Key, pair.Value);
                Assert.Equal(
                    SiloStatus.Active,
                    pair.Value);
            }
            Assert.Equal(expected, statuses.Count);
        }
    }

}

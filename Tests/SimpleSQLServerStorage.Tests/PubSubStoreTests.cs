using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using System.Diagnostics;
using System.IO;
using SimpleGrainInterfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Orleans;
using Orleans.Streams;

namespace SimpleSQLServerStorage.Tests
{
    [DeploymentItem("ClientConfigurationForTesting.xml")]
    [DeploymentItem("OrleansConfigurationForTesting.xml")]
    [DeploymentItem("OrleansProviders.dll")]
    [DeploymentItem("Orleans.StorageProviders.SimpleSQLServerStorage.dll")]
    [DeploymentItem("SimpleGrains.dll")]
    [DeploymentItem("PubSubStore.mdf")]
    [TestClass]
    public class PubSubStoreTests : TestCluster
    {
        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        public PubSubStoreTests()
            : base ( new TestClusterOptions(3)
            {
                 ClientConfiguration = { },
                  ClusterConfiguration = { PrimaryNode }
            }

            {
                StartFreshOrleans = true,
                SiloConfigFile = new FileInfo("OrleansConfigurationForTesting.xml"),
            },
            new TestingClientOptions()
            {
                ClientConfigFile = new FileInfo("ClientConfigurationForTesting.xml")
            })
        {
        }

        //[ClassCleanup]
        //public static void ClassCleanup()
        //{
        //    // Optional. 
        //    // By default, the next test class which uses TestignSiloHost will
        //    // cause a fresh Orleans silo environment to be created.
        //    StopAllSilos();
        //}



        [TestMethod]
        public void PubSubStoreTest()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public async Task StreamingPubSubStoreTest()
        {
            var strmId = Guid.NewGuid();

            var streamProv = GrainClient.GetStreamProvider("SMSProvider");
            IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");

            StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                (e, t) => { return TaskDone.Done; },
                e => { return TaskDone.Done; });
        }





    }
}

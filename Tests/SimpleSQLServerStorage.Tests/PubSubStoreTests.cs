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
    public class PubSubStoreTests //: TestingSiloHost
    {
        public static TestingSiloHost testingHost;


        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        //public PubSubStoreTests()
        //    : base(new TestingSiloOptions
        //    {
        //        SiloConfigFile = new FileInfo("OrleansConfigurationForTesting.xml"),
        //        StartFreshOrleans = true,

        //        AdjustConfig = config =>
        //        {

        //            config.Globals.RegisterStorageProvider<Orleans.StorageProviders.SimpleSQLServerStorage.SimpleSQLServerStorage>(providerName: "PubSubStore", properties:
        //                new Dictionary<string, string>                        {
        //                    //{ "ConnectionString" , @"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\PubSubStore.mdf;Trusted_Connection=Yes" },
        //                    { "ConnectionString" , string.Format(@"Data Source=(LocalDB)\v11.0;AttachDbFilename={0}\PubSubStore.mdf;Trusted_Connection=Yes",
        //                    TestContext.DeploymentDirectory)},
        //                    { "TableName", "lllll"},
        //                    { "UseJsonFormat", "both" }
        //                });

        //            //config.Globals.ServiceId = serviceId;
        //        }


        //    },
        //    new TestingClientOptions()
        //    {
        //        ClientConfigFile = new FileInfo("ClientConfigurationForTesting.xml")
        //    })
        //{}

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            AppDomain.CurrentDomain.SetData(
                "DataDirectory",
                context.TestDeploymentDir);


            testingHost = new TestingSiloHost(new TestingSiloOptions
            {
                SiloConfigFile = new FileInfo("OrleansConfigurationForTesting.xml"),
                StartFreshOrleans = true,

                AdjustConfig = config =>
                {

                    config.Globals.RegisterStorageProvider<Orleans.StorageProviders.SimpleSQLServerStorage.SimpleSQLServerStorage>(providerName: "PubSubStore", properties:
                        new Dictionary<string, string>                        {
                            //{ "ConnectionString" , @"Data Source=(LocalDB)\v11.0;AttachDbFilename=|DataDirectory|\PubSubStore.mdf;Trusted_Connection=Yes" },
                            { "ConnectionString" , string.Format(@"Data Source=(LocalDB)\v11.0;AttachDbFilename={0};Trusted_Connection=Yes", Path.Combine(context.DeploymentDirectory, "PubSubStore.mdf"))},
                            { "TableName", "lllll"},
                            { "UseJsonFormat", "both" }
                        });

                    //config.Globals.ServiceId = serviceId;
                }


            },
            new TestingClientOptions()
            {
                ClientConfigFile = new FileInfo("ClientConfigurationForTesting.xml")
            });



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

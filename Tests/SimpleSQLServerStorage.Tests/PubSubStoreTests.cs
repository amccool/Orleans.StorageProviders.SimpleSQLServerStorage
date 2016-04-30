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
using Orleans.TestingHost.Utils;
using Orleans.Runtime;

namespace SimpleSQLServerStorage.Tests
{
    [DeploymentItem("ClientConfigurationForTesting.xml")]
    [DeploymentItem("OrleansConfigurationForTesting.xml")]
    [DeploymentItem("OrleansProviders.dll")]
    [DeploymentItem("Orleans.StorageProviders.SimpleSQLServerStorage.dll")]
    [DeploymentItem("SimpleGrains.dll")]
    // EF is creating this file for us [DeploymentItem("PubSubStore.mdf")]
    [TestClass]
    public class PubSubStoreTests
    {
        public static TestingSiloHost testingHost;
        public static Logger logger;


        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [TestInitialize()]
        public void Initialize()
        { }

        [TestCleanup()]
        public void Cleanup()
        { }



        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            testingHost = new TestingSiloHost(new TestingSiloOptions
            {
                SiloConfigFile = new FileInfo("OrleansConfigurationForTesting.xml"),
                StartFreshOrleans = true,

                AdjustConfig = config =>
                {
                    config.Globals.RegisterStorageProvider<Orleans.StorageProviders.SimpleSQLServerStorage.SimpleSQLServerStorage>(providerName: "PubSubStore", properties:
                        new Dictionary<string, string>                        {
                            { "ConnectionString" , string.Format(@"Data Source=(localdb)\MSSQLLocalDB;AttachDbFilename={0};Trusted_Connection=Yes", 
                                Path.Combine(context.DeploymentDirectory, "PubSubStore.mdf"))},
                            { "TableName", "lllll"},
                            { "UseJsonFormat", "both" }
                        });
                }
            },
            new TestingClientOptions()
            {
                ClientConfigFile = new FileInfo("ClientConfigurationForTesting.xml")
            });

            logger = GrainClient.Logger;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // Optional. 
            // By default, the next test class which uses TestignSiloHost will
            // cause a fresh Orleans silo environment to be created.
            testingHost.StopAllSilos();
        }



        [TestMethod]
        public async Task PubSubStoreTest()
        {
            var streamGuid = Guid.NewGuid();
            string streamNamespace = "xxxx";
            string streamProviderName = "SMSProvider";

            // get producer and consumer
            var producer = GrainClient.GrainFactory.GetGrain<IStreamerOutGrain>(Guid.NewGuid());
            var consumer = GrainClient.GrainFactory.GetGrain<IStreamerInGrain>(Guid.NewGuid());

            // setup two subscriptions
            StreamSubscriptionHandle<int> firstSubscriptionHandle = await consumer.BecomeConsumer(streamGuid, streamNamespace, streamProviderName);
            StreamSubscriptionHandle<int> secondSubscriptionHandle = await consumer.BecomeConsumer(streamGuid, streamNamespace, streamProviderName);

            // produce some messages
            await producer.BecomeProducer(streamGuid, streamNamespace, streamProviderName);

            await producer.StartPeriodicProducing();
            await Task.Delay(TimeSpan.FromMilliseconds(1000));
            await producer.StopPeriodicProducing();

            // check
            await TestingUtils.WaitUntilAsync(lastTry => CheckCounters(producer, consumer, 2, lastTry), Timeout);

            // unsubscribe
            await consumer.StopConsuming(firstSubscriptionHandle);
            await consumer.StopConsuming(secondSubscriptionHandle);
        }


        private async Task<bool> CheckCounters(IStreamerOutGrain producer, IStreamerInGrain consumer, int consumerCount, bool assertIsTrue)
        {
            var numProduced = await producer.GetNumberProduced();
            var numConsumed = await consumer.GetNumberConsumed();
            if (assertIsTrue)
            {
                Assert.IsTrue(numConsumed.Values.All(v => v.Item2 == 0), "Errors");
                Assert.IsTrue(numProduced > 0, "Events were not produced");
                Assert.AreEqual(consumerCount, numConsumed.Count, "Incorrect number of consumers");
                foreach (int consumed in numConsumed.Values.Select(v => v.Item1))
                {
                    Assert.AreEqual(numProduced, consumed, "Produced and consumed counts do not match");
                }
            }
            else if (numProduced <= 0 || // no events produced?
                     consumerCount != numConsumed.Count || // subscription counts are wrong?
                     numConsumed.Values.Any(consumedCount => consumedCount.Item1 != numProduced) ||// consumed events don't match produced events for any subscription?
                     numConsumed.Values.Any(v => v.Item2 != 0)) // stream errors
            {
                if (numProduced <= 0)
                {
                    logger.Info("numProduced <= 0: Events were not produced");
                }
                if (consumerCount != numConsumed.Count)
                {
                    logger.Info("consumerCount != numConsumed.Count: Incorrect number of consumers. consumerCount = {0}, numConsumed.Count = {1}",
                        consumerCount, numConsumed.Count);
                }
                foreach (var consumed in numConsumed)
                {
                    if (numProduced != consumed.Value.Item1)
                    {
                        logger.Info("numProduced != consumed: Produced and consumed counts do not match. numProduced = {0}, consumed = {1}",
                            numProduced, consumed.Key.HandleId + " -> " + consumed.Value);
                        //numProduced, Utils.DictionaryToString(numConsumed));
                    }
                }
                return false;
            }
            logger.Info("All counts are equal. numProduced = {0}, numConsumed = {1}", numProduced,
                Utils.EnumerableToString(numConsumed, kvp => kvp.Key.HandleId.ToString() + "->" + kvp.Value.ToString()));
            return true;
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

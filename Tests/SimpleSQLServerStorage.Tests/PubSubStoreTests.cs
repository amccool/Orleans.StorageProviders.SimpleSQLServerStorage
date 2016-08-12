using System;
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
using Orleans.Runtime.Configuration;
using Xunit;
using Xunit.Abstractions;
using Orleans.StorageProviders.SimpleSQLServerStorage;
using System.Data.SqlLocalDb;
using System.Data.SqlClient;

namespace SimpleSQLServerStorage.Tests
{
    public class PubSubStoreTests : TestClusterPerTestInitializeAfterConstructor, IDisposable
    {
        private readonly string dbInstanceName;
        private readonly ISqlLocalDbInstance instance;
        private readonly Dictionary<string, string> dbNames;

        private static readonly string StreamProviderName = "sms";

        #region Orleans Stuff

        private readonly TimeSpan _timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        private readonly ITestOutputHelper output;

        public PubSubStoreTests(ITestOutputHelper output) : base()
        {
            this.output = output;

            var rnd = new Random();

            dbNames = new Dictionary<string, string>()
            {
                { "PubSubStore", rnd.Next().ToString() },
            };

            dbInstanceName = rnd.Next().ToString();

            ISqlLocalDbProvider provider = new SqlLocalDbProvider();
            instance = provider.GetOrCreateInstance(dbInstanceName);

            instance.Start();

            Initialize();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    // Optional. 
                    // By default, the next test class which uses TestignSiloHost will
                    // cause a fresh Orleans silo environment to be created.
                    this.HostedCluster.StopAllSilos();

                    instance.Stop();
                    ISqlLocalDbApi localDB = new SqlLocalDbApiWrapper();
                    localDB.DeleteInstance(instance.Name);

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FacilityGrainTest() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion



        public override TestCluster CreateTestCluster()
        {
            var options = new TestClusterOptions();

            options.ClientConfiguration.AddSimpleMessageStreamProvider(StreamProviderName, false);
            options.ClusterConfiguration.AddSimpleMessageStreamProvider(StreamProviderName, false);


            foreach (var item in dbNames)
            {
                string actualDBName = item.Value;
                string dbName = item.Key;
                using (SqlConnection connection = instance.CreateConnection())
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand($"create database [{actualDBName}]", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                var sbuilder = instance.CreateConnectionStringBuilder();
                sbuilder.SetInitialCatalogName(actualDBName);

                options.ClusterConfiguration.AddSimpleSQLStorageProvider(dbName, sbuilder.ConnectionString, "true");
            }


            return new TestCluster(options);
        }
        #endregion

        [Fact]
        public async Task PubSubStoreTest()
        {
            var streamGuid = Guid.NewGuid();
            string streamNamespace = "xxxx";
            string streamProviderName = StreamProviderName;

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
            await TestingUtils.WaitUntilAsync(lastTry => CheckCounters(producer, consumer, 2, lastTry), _timeout);

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
                Assert.True(numConsumed.Values.All(v => v.Item2 == 0), "Errors");
                Assert.True(numProduced > 0, "Events were not produced");
                Assert.Equal(consumerCount, numConsumed.Count);//, "Incorrect number of consumers");
                foreach (int consumed in numConsumed.Values.Select(v => v.Item1))
                {
                    Assert.Equal(numProduced, consumed);//, "Produced and consumed counts do not match");
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


        [Fact]
        public async Task StreamingPubSubStoreTest()
        {
            var strmId = Guid.NewGuid();

            var providers = this.HostedCluster.ClientConfiguration.GetAllProviderConfigurations();
            var sms = from x in providers
                      where
  x.Name.Equals(StreamProviderName)
                      select x;

            Assert.True(sms.Count() == 1);


            try
            {
                var streamProv = GrainClient.GetStreamProvider(StreamProviderName);
                IAsyncStream<int> stream = streamProv.GetStream<int>(strmId, "test1");

                StreamSubscriptionHandle<int> handle = await stream.SubscribeAsync(
                    (e, t) => { return TaskDone.Done; },
                    e => { return TaskDone.Done; });
            }
            catch(Exception ex)
            {
                this.output.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}

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
    // EF is creating this file for us     [DeploymentItem("basic.mdf")]
    [TestClass]
    public class GrainStorageTests
    {
        public static TestingSiloHost testingHost;

        private readonly TimeSpan timeout = Debugger.IsAttached ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(10);

        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            testingHost = new TestingSiloHost(new TestingSiloOptions
            {
                SiloConfigFile = new FileInfo("OrleansConfigurationForTesting.xml"),
                StartFreshOrleans = true,

                AdjustConfig = config =>
                {
                    config.Globals.RegisterStorageProvider<Orleans.StorageProviders.SimpleSQLServerStorage.SimpleSQLServerStorage>(providerName: "basic", properties:
                        new Dictionary<string, string>                        {
                            { "ConnectionString" , string.Format(@"Data Source=(localdb)\MSSQLLocalDB;AttachDbFilename={0};Trusted_Connection=Yes", Path.Combine(context.DeploymentDirectory, "basic.mdf"))},
                            { "TableName", "basic"},
                            { "UseJsonFormat", "both" }
                        });
                }
            },
            new TestingClientOptions()
            {
                ClientConfigFile = new FileInfo("ClientConfigurationForTesting.xml")
            });
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
        public async Task TestMethodGetAGrainTest()
        {
            var g = testingHost.GrainFactory.GetGrain<IMyGrain>(0);

            await g.SaveSomething(1, "ff", Guid.NewGuid(), DateTime.Now, new int[] { 1, 2, 3, 4, 5 });
        }



        [TestMethod]
        public async Task TestGrains()
        {
            var rnd = new Random();
            var rndId1 = rnd.Next();
            var rndId2 = rnd.Next();



            // insert your grain test code here
            var grain = testingHost.GrainFactory.GetGrain<IMyGrain>(rndId1);

            var thing4 = new DateTime();
            var thing3 = Guid.NewGuid();
            var thing1 = 1;
            var thing2 = "ggggggggggggggggg";
            var things = new List<int> { 5, 6, 7, 8, 9 };

            await grain.SaveSomething(thing1, thing2, thing3, thing4, things);

            Assert.AreEqual(thing1, await grain.GetThing1());
            Assert.AreEqual(thing2, await grain.GetThing2());
            Assert.AreEqual(thing3, await grain.GetThing3());
            Assert.AreEqual(thing4, await grain.GetThing4());
            var res = await grain.GetThings1();
            CollectionAssert.AreEqual(things, res.ToList());
        }



        [TestMethod]
        public async Task StateTestGrainTest()
        {
            var rnd = new Random();
            var rndId1 = rnd.Next();
            var rndId2 = rnd.Next();



            // insert your grain test code here
            var grain = testingHost.GrainFactory.GetGrain<IStateTestGrain>(rndId1);

            var thing4 = new DateTime();
            var thing3 = Guid.NewGuid();
            var thing1 = 1;
            var thing2 = "ggggggggggggggggg";
            var things = new List<int> { 5, 6, 7, 8, 9 };

            await grain.SaveSomething(thing1, thing2, thing3, thing4, things);

            Assert.AreEqual(thing1, await grain.GetThing1());
            Assert.AreEqual(thing2, await grain.GetThing2());
            Assert.AreEqual(thing3, await grain.GetThing3());
            Assert.AreEqual(thing4, await grain.GetThing4());
            var res = await grain.GetThings1();
            CollectionAssert.AreEqual(things, res.ToList());
        }


    }
}

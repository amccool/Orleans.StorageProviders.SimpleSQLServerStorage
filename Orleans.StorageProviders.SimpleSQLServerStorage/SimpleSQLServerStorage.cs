using Newtonsoft.Json;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Orleans.Serialization;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Migrations;

namespace Orleans.StorageProviders.SimpleSQLServerStorage
{
    /// <summary>
    /// KeyValue Storage of grain state
    /// UseJsonFormat defaults to false, but can be set to true, false, or both
    /// if both is set, then both binary and json data is stored, but the binary data is used
    /// </summary>
    public class SimpleSQLServerStorage : IStorageProvider
    {
        private SqlConnectionStringBuilder sqlconnBuilder;

        private const string CONNECTION_STRING = "ConnectionString";
        private const string USE_JSON_FORMAT_PROPERTY = "UseJsonFormat";

        private string serviceId;
        private Newtonsoft.Json.JsonSerializerSettings jsonSettings;
        private StorageFormatEnum useJsonOrBinaryFormat;


        /// <summary> Name of this storage provider instance. </summary>
        /// <see cref="IProvider#Name"/>
        public string Name { get; private set; }

        /// <summary> Logger used by this storage provider instance. </summary>
        /// <see cref="IStorageProvider#Log"/>
        public Logger Log { get; private set; }


        /// <summary> Initialization function for this storage provider. </summary>
        /// <see cref="IProvider#Init"/>
        public async Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            Name = name;
            serviceId = providerRuntime.ServiceId.ToString();

            if (!config.Properties.ContainsKey(CONNECTION_STRING) ||
                string.IsNullOrWhiteSpace(config.Properties[CONNECTION_STRING]))
            {
                throw new ArgumentException("Specify a value for:", CONNECTION_STRING);
            }
            var connectionString = config.Properties[CONNECTION_STRING];
            sqlconnBuilder = new SqlConnectionStringBuilder(connectionString);

            //a validation of the connection would be wise to perform here
            //await new SqlConnection(sqlconnBuilder.ConnectionString).OpenAsync();

            //initialize to use the default of JSON storage (this is to provide backwards compatiblity with previous version
            useJsonOrBinaryFormat = StorageFormatEnum.Binary;

            if (config.Properties.ContainsKey(USE_JSON_FORMAT_PROPERTY))
            {
                if ("true".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase))
                    useJsonOrBinaryFormat = StorageFormatEnum.Json;

                if ("both".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase))
                    useJsonOrBinaryFormat = StorageFormatEnum.Both;
            }

            jsonSettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
            };

            Log = providerRuntime.GetLogger("StorageProvider.SimpleSQLServerStorage." + serviceId);
        }

        // Internal method to initialize for testing
        internal void InitLogger(Logger logger)
        {
            Log = logger;
        }

        /// <summary> Shutdown this storage provider. </summary>
        /// <see cref="IStorageProvider#Close"/>
        public Task Close()
        {
            //the using of the dbContext of the async methods finally should be disposing the connection
            return TaskDone.Done;
        }

        /// <summary> Read state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#ReadStateAsync"/>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();

            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvide_ReadingData,
                    "Reading: GrainType={0} Pk={1} Grainid={2} from DataSource={3}",
                    grainType, primaryKey, grainReference, this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog);
            }

            //var data = new Dictionary<string, object>();

            using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
            {
                switch (this.useJsonOrBinaryFormat)
                {
                    case StorageFormatEnum.Binary:
                    case StorageFormatEnum.Both:
                        {
                            var value = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => s.BinaryContent).SingleOrDefaultAsync();
                            if (value != null)
                            {
                                //data = SerializationManager.DeserializeFromByteArray<Dictionary<string, object>>(value);
                                grainState.State = SerializationManager.DeserializeFromByteArray<object>(value);
                            }
                        }
                        break;
                    case StorageFormatEnum.Json:
                        {
                            var value = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => s.JsonContext).SingleOrDefaultAsync();
                            if (!string.IsNullOrEmpty(value))
                            {
                                //data = JsonConvert.DeserializeObject<Dictionary<string, object>>(value, jsonSettings);
                                grainState.State = JsonConvert.DeserializeObject(value, grainState.State.GetType(), jsonSettings);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //grainState.SetAll(data);

            grainState.ETag = Guid.NewGuid().ToString();
        }













        /// <summary> Write state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#WriteStateAsync"/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvide_WritingData,
                    "Writing: GrainType={0} PrimaryKey={1} Grainid={2} ETag={3} to DataSource={4}",
                    grainType, primaryKey, grainReference, grainState.ETag, this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog);
            }
            //var data = grainState.AsDictionary();
            var data = grainState.State;

            byte[] payload = null;
            string jsonpayload = string.Empty;

            if (this.useJsonOrBinaryFormat != StorageFormatEnum.Json)
            {
                payload = SerializationManager.SerializeToByteArray(data);
            }

            if (this.useJsonOrBinaryFormat == StorageFormatEnum.Json || this.useJsonOrBinaryFormat == StorageFormatEnum.Both)
            {
                jsonpayload = JsonConvert.SerializeObject(data, jsonSettings);
            }


            //await redisDatabase.StringSetAsync(primaryKey, json);
            var kvb = new KeyValueStore()
            {
                JsonContext = jsonpayload,
                BinaryContent = payload,
                GrainKeyId = primaryKey,
            };

            using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
            {
                db.Set<KeyValueStore>().AddOrUpdate(kvb);
                await db.SaveChangesAsync();
            }
        }

        /// <summary> Clear state data function for this storage provider. </summary>
        /// <remarks>
        /// </remarks>
        /// <see cref="IStorageProvider#ClearStateAsync"/>
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerStorageProvider_ClearingData,
                    "Clearing: GrainType={0} Pk={1} Grainid={2} ETag={3} DeleteStateOnClear={4} from DataSource={5}",
                    grainType, primaryKey, grainReference, grainState.ETag, this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog);
            }
            var entity = new KeyValueStore() { GrainKeyId = primaryKey };
            using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
            {
                db.KeyValues.Attach(entity);
                db.KeyValues.Remove(entity);
                await db.SaveChangesAsync();
            }
        }
    }
}

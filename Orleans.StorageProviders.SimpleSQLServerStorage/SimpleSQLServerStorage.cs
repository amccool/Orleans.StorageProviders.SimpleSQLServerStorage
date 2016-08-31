using Newtonsoft.Json;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using Orleans.Serialization;
using System.Data.Entity.Migrations;
using System.Globalization;
using System.Text;

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
            serviceId = providerRuntime.ServiceId.ToString();
            Log = providerRuntime.GetLogger("StorageProvider.SimpleSQLServerStorage." + serviceId);

            try
            {
                Name = name;
                this.jsonSettings = SerializationManager.UpdateSerializerSettings(SerializationManager.GetDefaultJsonSerializerSettings(), config);

                if (!config.Properties.ContainsKey(CONNECTION_STRING) || string.IsNullOrWhiteSpace(config.Properties[CONNECTION_STRING]))
                {
                    throw new BadProviderConfigException($"Specify a value for: {CONNECTION_STRING}");
                }
                var connectionString = config.Properties[CONNECTION_STRING];
                sqlconnBuilder = new SqlConnectionStringBuilder(connectionString);

                //a validation of the connection would be wise to perform here
                var sqlCon = new SqlConnection(sqlconnBuilder.ConnectionString);
                await sqlCon.OpenAsync();
                sqlCon.Close();

                //initialize to use the default of JSON storage (this is to provide backwards compatiblity with previous version
                useJsonOrBinaryFormat = StorageFormatEnum.Binary;

                if (config.Properties.ContainsKey(USE_JSON_FORMAT_PROPERTY))
                {
                    if ("true".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase))
                        useJsonOrBinaryFormat = StorageFormatEnum.Json;

                    if ("both".Equals(config.Properties[USE_JSON_FORMAT_PROPERTY], StringComparison.OrdinalIgnoreCase))
                        useJsonOrBinaryFormat = StorageFormatEnum.Both;
                }
            }
            catch (Exception ex)
            {
                Log.Error((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_InitProvider, ex.ToString(), ex);
                throw;
            }
        }

        /// <summary> Shutdown this storage provider. </summary>
        /// <see cref="IStorageProvider#Close"/>
        public async Task Close()
        { }

        /// <summary> Read state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#ReadStateAsync"/>
        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();

            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_ReadingData,
                    $"Reading: GrainType={grainType} Pk={primaryKey} Grainid={grainReference} from DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}");
            }

            try
            {
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

                    var etagText = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => s.Etag).SingleOrDefaultAsync();
                    if (!string.IsNullOrEmpty(etagText))
                    {
                        grainState.ETag = etagText;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error((int)SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_ReadError,
                    $"Error reading: GrainType={grainType} Grainid={grainReference} ETag={grainState.ETag} from DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}",
                    ex);
                throw;
            }

        }


        /// <summary> 
        /// Write state data function for this storage provider.
        /// </summary>
        /// <see cref="IStorageProvider#WriteStateAsync"/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_WritingData,
                    $"Writing: GrainType={grainType} PrimaryKey={primaryKey} GrainId={grainReference} ETag={grainState.ETag} to DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}");
            }
            try
            {
                using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
                {
                    var etagText = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => s.Etag).SingleOrDefaultAsync();
                    if (string.IsNullOrEmpty(etagText)||string.IsNullOrEmpty(grainState.ETag))
                    {
                        //no etag present
                        //allow the write
                    }
                    else
                    {
                        if (grainState.ETag.Equals(etagText))
                        {
                            //all is well in the world, the updating record matches the etag of the target record.
                            //allow the write
                        }
                        else
                        {
                            //mismatch , throw invalid etag
                            throw new InconsistentStateException("etag mismatch", etagText, grainState.ETag);
                        }
                    }
                }

                var data = grainState.State;

                byte[] payload = null;
                string jsonpayload = string.Empty;
                string eTagForWrite = string.Empty;

                if (this.useJsonOrBinaryFormat != StorageFormatEnum.Json)
                {
                    payload = SerializationManager.SerializeToByteArray(data);
                    eTagForWrite = GenerateETag(payload);
                }

                if (this.useJsonOrBinaryFormat == StorageFormatEnum.Json || this.useJsonOrBinaryFormat == StorageFormatEnum.Both)
                {
                    jsonpayload = JsonConvert.SerializeObject(data, jsonSettings);
                    eTagForWrite = GenerateETag(jsonpayload);
                }

                //we really need to be writing an Etag to the db as well
                var kvb = new KeyValueStore()
                {
                    JsonContext = jsonpayload,
                    BinaryContent = payload,
                    GrainKeyId = primaryKey,
                    Etag = eTagForWrite
                };

                using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
                {
                    db.Set<KeyValueStore>().AddOrUpdate(kvb);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_WriteError,
                    $"Error writing: GrainType={grainType} GrainId={grainReference} ETag={grainState.ETag} to DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}",
                    ex);
                throw;
            }
        }

        private string GenerateETag(string jsonpayload)
        {
            var md5Hasher = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(jsonpayload));
            return BitConverter.ToString(data);
        }

        private string GenerateETag(byte[] payload)
        {
            var md5Hasher = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hasher.ComputeHash(payload);
            return BitConverter.ToString(data);
        }


        /// <summary> Clear state data function for this storage provider. </summary>
        /// <remarks>
        /// </remarks>
        /// <see cref="IStorageProvider#ClearStateAsync"/>
        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var primaryKey = grainReference.ToKeyString();

            try
            {
                if (Log.IsVerbose3)
                {
                    Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerStorageProvider_ClearingData,
                        $"Clearing: GrainType={grainType} Pk={primaryKey} Grainid={grainReference} ETag={grainState.ETag} from DataSource={this.sqlconnBuilder.DataSource} Catalog={this.sqlconnBuilder.InitialCatalog}");
                }
                var entity = new KeyValueStore() { GrainKeyId = primaryKey };
                using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
                {
                    db.KeyValues.Attach(entity);
                    db.KeyValues.Remove(entity);
                    await db.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                Log.Error((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_DeleteError,
                  $"Error clearing: GrainType={grainType} GrainId={grainReference} ETag={grainState.ETag} in to DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}",
                  ex);

                throw;
            }
        }


        private void ValidateEtag(string currentETag, string receivedEtag, string grainStoreKey, string operation)
        {
            // if we have no current etag, we will accept the users data.
            // This is a mitigation for when the memory storage grain is lost due to silo crash.
            if (currentETag == null)
                return;

            // if this is our first write, and we have an empty etag, we're good
            if (string.IsNullOrEmpty(currentETag) && receivedEtag == null)
                return;

            // if current state and new state have matching etags, we're good
            if (receivedEtag == currentETag)
                return;

            // else we have an etag mismatch
            string error = $"Etag mismatch during {operation} for grain {grainStoreKey}: Expected = {currentETag ?? "null"} Received = {receivedEtag}";
            Log.Warn(0, error);
            throw new InconsistentStateException(error);
        }

    }
}
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

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
                this.jsonSettings = OrleansJsonSerializer.UpdateSerializerSettings(OrleansJsonSerializer.GetDefaultSerializerSettings(), config);

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
                useJsonOrBinaryFormat = StorageFormatEnum.Json;

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
        public Task Close()
        {
            return TaskDone.Done;
        }

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
                            {
                                var value = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => new { s.BinaryContent, s.ETag }).SingleOrDefaultAsync();
                                if (value != null)
                                {
                                    grainState.State = SerializationManager.DeserializeFromByteArray<object>(value.BinaryContent);
									grainState.ETag = value.ETag;
									if(grainState.State is GrainState)
										((GrainState)grainState.State).Etag = value.ETag;
								}
							}
							break;
                        case StorageFormatEnum.Json:
                        case StorageFormatEnum.Both:
                            {
                                var value = await db.KeyValues
                        .Where(s => s.GrainKeyId.Equals(primaryKey))
                        .Select(s => new { s.JsonContext, s.ETag }).SingleOrDefaultAsync();
                                if (value != null && !string.IsNullOrEmpty(value.JsonContext))
                                {
                                    grainState.State = JsonConvert.DeserializeObject(value.JsonContext, grainState.State.GetType(), jsonSettings);
									grainState.ETag = value.ETag;
									if(grainState.State is GrainState)
										((GrainState)grainState.State).Etag = value.ETag;
								}
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_ReadError,
                    $"Error reading: GrainType={grainType} Grainid={grainReference} ETag={grainState.ETag} from DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}",
                    ex);
                throw;
            }

        }


        /// <summary> Write state data function for this storage provider. </summary>
        /// <see cref="IStorageProvider#WriteStateAsync"/>
        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
			if(grainState.State is GrainState)	//WARN: HACK, such a hack
				grainState.ETag = ((GrainState)grainState.State).Etag;

			var primaryKey = grainReference.ToKeyString();
            if (Log.IsVerbose3)
            {
                Log.Verbose3((int) SimpleSQLServerProviderErrorCodes.SimpleSQLServerProvider_WritingData,
                    $"Writing: GrainType={grainType} PrimaryKey={primaryKey} GrainId={grainReference} ETag={grainState.ETag} to DataSource={this.sqlconnBuilder.DataSource + "." + this.sqlconnBuilder.InitialCatalog}");
            }
            try
            {
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

				var kvb = new KeyValueStore()
                {
                    JsonContext = jsonpayload,
                    BinaryContent = payload,
                    GrainKeyId = primaryKey,
					ETag = String.IsNullOrEmpty(jsonpayload) ? payload.CalculateMD5Hash(): jsonpayload.CalculateMD5Hash()
                };

                using (var db = new KeyValueDbContext(this.sqlconnBuilder.ConnectionString))
                {
					if(grainState.ETag != null)
					{
						var value = await db.KeyValues.Where(s => s.GrainKeyId.Equals(primaryKey)).Select(s => s.ETag).SingleOrDefaultAsync();
						if(value != null && value != grainState.ETag)
						{
							string error = $"Etag mismatch during WriteStateAsync for grain {primaryKey}: Expected = {value ?? "null"} Received = {grainState.ETag}";
							Log.Error(0, error);
							throw new InconsistentStateException(error);
						}
					}

                    db.Set<KeyValueStore>().AddOrUpdate(kvb);

					grainState.ETag = kvb.ETag;
					if(grainState.State is GrainState)
						((GrainState)grainState.State).Etag = kvb.ETag;

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

					grainState.ETag = null;
					if(grainState.State is GrainState)
						((GrainState)grainState.State).Etag = null;

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
    }

    public static class ProviderExtenstions
    {
        public static string CalculateMD5Hash(this string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static string CalculateMD5Hash(this byte[] inputBytes)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }


    }
}
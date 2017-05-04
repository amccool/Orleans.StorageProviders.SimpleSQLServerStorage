using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;

namespace Orleans.StorageProviders.SimpleSQLServerStorage
{
    public class KeyValueDbConfiguration : DbConfiguration
    {
        public KeyValueDbConfiguration()
        {
            this.SetProviderServices(
                SqlProviderServices.ProviderInvariantName,            
                SqlProviderServices.Instance);

            this.SetExecutionStrategy(SqlProviderServices.ProviderInvariantName, () => new SqlAzureExecutionStrategy(2, TimeSpan.FromMilliseconds(100)));

            this.SetDefaultConnectionFactory(new SqlConnectionFactory());
        }
    }
}
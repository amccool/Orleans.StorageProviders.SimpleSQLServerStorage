using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.StorageProviders.SimpleSQLServerStorage
{
    internal enum SimpleSQLServerProviderErrorCodes
    {
        ProvidersBase = 400000,
        
        SimpleSQLServerProviderBase                      = ProvidersBase + 100,
        SimpleSQLServerProvider_DataNotFound             = SimpleSQLServerProviderBase + 1,
        SimpleSQLServerProvider_ReadingData              = SimpleSQLServerProviderBase + 2,
        SimpleSQLServerProvider_WritingData              = SimpleSQLServerProviderBase + 3,
        SimpleSQLServerProvider_Storage_Reading          = SimpleSQLServerProviderBase + 4,
        SimpleSQLServerProvider_Storage_Writing          = SimpleSQLServerProviderBase + 5,
        SimpleSQLServerProvider_Storage_DataRead         = SimpleSQLServerProviderBase + 6,
        SimpleSQLServerProvider_WriteError               = SimpleSQLServerProviderBase + 7,
        SimpleSQLServerProvider_DeleteError              = SimpleSQLServerProviderBase + 8,
        SimpleSQLServerProvider_InitProvider             = SimpleSQLServerProviderBase + 9,
        SimpleSQLServerProvider_ParamConnectionString    = SimpleSQLServerProviderBase + 10,
        SimpleSQLServerProvider_ReadError                = SimpleSQLServerProviderBase + 11,
        SimpleSQLServerProvider_ClearingData             = SimpleSQLServerProviderBase + 12,
    }
}

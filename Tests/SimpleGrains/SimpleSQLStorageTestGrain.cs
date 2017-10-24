using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrains
{
    [Serializable]
    public class PersistenceTestGrainState
    {
        public PersistenceTestGrainState()
        {
            SortedDict = new SortedDictionary<int, int>();
        }

        public int Field1 { get; set; }
        public string Field2 { get; set; }
        public SortedDictionary<int, int> SortedDict { get; set; }
    }

    [Serializable]
    public class PersistenceGenericGrainState<T>
    {
        public T Field1 { get; set; }
        public string Field2 { get; set; }
        public SortedDictionary<T, T> SortedDict { get; set; }
    }



















    [Orleans.Providers.StorageProvider(ProviderName = "SimpleSQLStore")]
    public class SimpleSQLStorageTestGrain : Grain<PersistenceTestGrainState>,
        ISimpleSQLStorageTestGrain, ISimpleSQLStorageTestGrain_LongKey
    {
        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(State.Field1);
        }

        public Task DoWrite(int val)
        {
            State.Field1 = val;
            return WriteStateAsync();
        }

        public async Task<int> DoRead()
        {
            await ReadStateAsync(); // Re-read state from store
            return State.Field1;
        }

        public Task DoDelete()
        {
            return ClearStateAsync(); // Automatically marks this grain as DeactivateOnIdle 
        }
    }

    [Orleans.Providers.StorageProvider(ProviderName = "SimpleSQLStore")]
    public class SimpleSQLStorageGenericGrain<T> : Grain<PersistenceGenericGrainState<T>>,
        ISimpleSQLStorageGenericGrain<T>
    {
        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public Task<T> GetValue()
        {
            return Task.FromResult(State.Field1);
        }

        public Task DoWrite(T val)
        {
            State.Field1 = val;
            return WriteStateAsync();
        }

        public async Task<T> DoRead()
        {
            await ReadStateAsync(); // Re-read state from store
            return State.Field1;
        }

        public Task DoDelete()
        {
            return ClearStateAsync(); // Automatically marks this grain as DeactivateOnIdle 
        }
    }

    [Orleans.Providers.StorageProvider(ProviderName = "SimpleSQLStore")]
    public class SimpleSQLStorageTestGrainExtendedKey : Grain<PersistenceTestGrainState>,
        ISimpleSQLStorageTestGrain_GuidExtendedKey, ISimpleSQLStorageTestGrain_LongExtendedKey
    {
        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public Task<int> GetValue()
        {
            return Task.FromResult(State.Field1);
        }

        public Task<string> GetExtendedKeyValue()
        {
            string extKey;
            var pk = this.GetPrimaryKey(out extKey);
            return Task.FromResult(extKey);
        }

        public Task DoWrite(int val)
        {
            State.Field1 = val;
            return WriteStateAsync();
        }

        public async Task<int> DoRead()
        {
            await ReadStateAsync(); // Re-read state from store
            return State.Field1;
        }

        public Task DoDelete()
        {
            return ClearStateAsync(); // Automatically marks this grain as DeactivateOnIdle 
        }
    }

}

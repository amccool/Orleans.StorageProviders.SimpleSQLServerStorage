using Orleans;
using Orleans.Providers;
using SimpleGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrains
{

    [StorageProvider(ProviderName="basic")]
    public class StateTestGrain : Grain<StateTest>, IStateTestGrain
    {
        public Task<int> GetThing1()
        {
            this.ReadStateAsync();
            return Task.FromResult(this.State.Thing1);
        }

        public Task<string> GetThing2()
        {
            this.ReadStateAsync();
            return Task.FromResult(this.State.Thing2);
        }

        public Task<Guid> GetThing3()
        {
            this.ReadStateAsync();
            return Task.FromResult(this.State.Thing3);
        }

        public Task<DateTime> GetThing4()
        {
            this.ReadStateAsync();
            return Task.FromResult(this.State.Thing4);
        }

        public Task<IEnumerable<int>> GetThings1()
        {
            this.ReadStateAsync();
            return Task.FromResult(this.State.Things1);
        }


        public Task SaveSomething(int thing1, string thing2, Guid thing3, DateTime thing4, IEnumerable<int> things1)
        {
            State.Thing1 = thing1;
            State.Thing2 = thing2;
            State.Thing3 = thing3;
            State.Thing4 = thing4;
            State.Things1 = things1;
            return this.WriteStateAsync();
        }

        public Task SetThing1(int v)
        {
            State.Thing1 = v;
            return this.WriteStateAsync();
        }

        public Task SetThing2(string v)
        {
            State.Thing2 = v;
            return this.WriteStateAsync();
        }

        public Task SetThing3(Guid v)
        {
            State.Thing3 = v;
            return this.WriteStateAsync();
        }

        public Task SetThings1(IEnumerable<int> v)
        {
            State.Things1 = v;
            return this.WriteStateAsync();
        }
    }
}

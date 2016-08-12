using Orleans;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SimpleGrainInterfaces
{
    public interface IStateTestGrain : IGrainWithIntegerKey
    {
        Task SaveSomething(int thing1, string thing2, Guid thing3, DateTime thing4, IEnumerable<int> things1);

        Task<int> GetThing1();
        Task SetThing1(int v);

        Task<string> GetThing2();
        Task SetThing2(string v);

        Task<Guid> GetThing3();
        Task SetThing3(Guid v);

        Task<DateTime> GetThing4();

        Task<IEnumerable<int>> GetThings1();
        Task SetThings1(IEnumerable<int> v);

        Task ClearTheState();

        Task<IPAddress> GetIpAddr();
        Task SetIpAddr(IPAddress v);


    }
}
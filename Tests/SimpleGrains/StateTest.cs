using Orleans;
using Orleans.Providers;
using SimpleGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleGrains
{
    [Serializable]
    public class StateTest
    {
        public int Thing1 { get; set; }
        public string Thing2 { get; set; }
        public Guid Thing3 { get; set; }
        public DateTime Thing4 { get; set; }
        public IEnumerable<int> Things1 { get; set; }
        public IPAddress ipaddr { get; set; }
    }
}
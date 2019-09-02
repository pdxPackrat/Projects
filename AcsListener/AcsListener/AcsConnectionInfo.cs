using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    class AcsConnectionInfo
    {
        public bool ConnectedToAcs { get; set; }
        public IPAddress RemoteIpAddress { get; set; }
    }
}

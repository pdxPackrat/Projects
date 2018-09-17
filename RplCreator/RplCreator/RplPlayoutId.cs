using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RplCreator
{
    public static class RplPlayoutId
    {
        public static UInt32 GenerateNewId()
        {
            Random rand = new Random();
            return (uint)rand.Next(10000000, 99999999);  // technically an RPL PlayoutId is a Uint32 but we're simplifying
            // return (uint)rand.Next(10000000, int.MaxValue);  // technically an RPL PlayoutId is a Uint32 but we're simplifying
        }
    }
}

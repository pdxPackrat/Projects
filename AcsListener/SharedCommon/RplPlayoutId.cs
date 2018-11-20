using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCommon
{
    public static class RplPlayoutId
    {
        public static UInt32 GenerateNewId()
        {
            Random rand = new Random();
            return (uint)rand.Next(10000000, 99999999);  // technically an RPL PlayoutId is a Uint32 but we're simplifying
            // return (uint)rand.Next(10000000, int.MaxValue);  // technically an RPL PlayoutId is a Uint32 but we're simplifying
        }

        /// <summary>
        /// This method is the most dependable to produce a UNIQUE result for the PlayoutID, but
        /// the range WILL fall between the min and max values of a UINT32
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static UInt32 GenerateNewIdFromString(string inputString)
        {

            return (UInt32)inputString.GetHashCode();
        }
    }
}

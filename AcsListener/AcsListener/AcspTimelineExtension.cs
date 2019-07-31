using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable 169  // disable the CS0169 warning for the unused fields in AcspTimelineExtension

namespace AcsListener
{
    public enum TimelineExtensionKey : UInt32
    {
        CurrentCompositionPlaylistId = 0,       // KeyVal=0, Length=16bytes, Type=UUID, "UUID of current composition"
        CurrentCompositionPlaylistPosition = 1, // KeyVal=1, Length=8bytes, Type=UInt64, "Position within current composition"
        CurrentReelId = 2,                      // KeyVal=2, Length=16bytes, Type=UUID, "UUID of current reel"
        CurrentReelPosition = 3,                // KeyVal=3, Length=8bytes, Type=UInt64, "Position within the current reel"
        NextCompositionPlaylistId = 4,          // KeyVal=4, Length=16bytes, Type=UUID, "UUID of next composition"
        NextReelId = 5,                         // KeyVal=5, Length=16bytes, Type=UUID, "UUID of next reel"
                                                // KeyValues 6-255 are reserved, and >256 are available as user-defined extensions
    }

    public class AcspTimelineExtension
    {
        private Byte[] _key;
        private AcspBerLength _packLength;

        private Byte[] _value;
        // Leaving this uncompleted for now as we don't need it for the proof-of-concept
        // and I need to figure out the proper way to implement this, since the value completely changes based on which type of key is passed.
        // I may need to have a separate constructor method for each of the 6 existing key types, not sure how to do that as overloading won't
        // work there. 
    }
}

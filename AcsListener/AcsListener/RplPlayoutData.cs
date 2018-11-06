using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    /// <summary>
    /// RplPlayoutData represents the essential information that is needed to be kept regarding any RPL-related actions with the ACS.
    /// </summary>
    class RplPlayoutData
    {
        private UInt32 _playoutId;         // Unique Playout ID that is generated for an RPL at time of RPL file creation
        private UInt64 _timelineOffset;    // Probably won't be used in our implementation but see 430-10:2010, page 6 section 6.3.2.1 for more information
        private string _editRate;          // Analagous to the frame rate of the movie, but in this case represents the number of "ticks" per second for the caption time codes
        private string _resourceUrl;       // The URL path of the RPL file

        /// <summary>
        /// Basic constructor for the RplPlayoutData object
        /// </summary>
        public RplPlayoutData()
        {
            InitializeData();
        }

        /// <summary>
        /// InitializeData is responsible for initializing the main fields of the RplPlayoutData object
        /// </summary>
        private void InitializeData()
        {
            this._playoutId = 0;
            this._timelineOffset = 0;
            this._editRate = "";
            this._resourceUrl = "";
        }

        /// <summary>
        /// PlayoutId represents a unique unsigned 32-bit integer, ranged from 10000000 to 99999999, created
        /// at time of RPL file creation.
        /// </summary>
        public UInt32 PlayoutId
        {
            get
            {
                return _playoutId;
            }
            set
            {
                _playoutId = value;
            }
        }

        /// <summary>
        /// TimelineOffset property will rarely be used, but represents a manual offset in the start time of the RPL.
        /// Should almost always be 0.
        /// </summary>
        public UInt64 TimelineOffset
        {
            get
            {
                return _timelineOffset;
            }
            set
            {
                _timelineOffset = value;
            }
        }

        /// <summary>
        /// EditRate property represents the "tick rate" of the caption time codes.  An edit rate of "25 1" would mean 25
        /// "ticks" per second of caption, with each "tick" basically being 1/25 of a second, or 40ms in that example.
        /// </summary>
        public string EditRate
        {
            get
            {
                return _editRate;
            }
            set
            {
                _editRate = value;
            }
        }

        /// <summary>
        /// ResourceUrl property represents the URL path of the RPL file.
        /// </summary>
        public string ResourceUrl
        {
            get
            {
                return _resourceUrl;
            }
            set
            {
                _resourceUrl = value;
            }
        }
    }
}

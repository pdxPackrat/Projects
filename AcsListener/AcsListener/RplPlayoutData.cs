using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    class RplPlayoutData
    {
        private UInt32 _playoutId;
        private UInt64 _timelineOffset;    // Probably won't be used in our implementation but see 430-10:2010, page 6 section 6.3.2.1 for more information
        private string _editRate;
        private string _resourceUrl;

        public RplPlayoutData()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            this._playoutId = 0;
            this._timelineOffset = 0;
            this._editRate = "";
            this._resourceUrl = "";
        }

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

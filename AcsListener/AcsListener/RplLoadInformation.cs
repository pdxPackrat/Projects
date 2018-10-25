using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    class RplLoadInformation
    {
        private Dictionary<UInt32, RplPlayoutData> _loadInfo = new Dictionary<UInt32, RplPlayoutData>();
        private UInt32 _currentPlayoutId;

        public RplLoadInformation()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            _currentPlayoutId = 0;
        }

        public void InsertRplData(RplPlayoutData playoutData)
        {
            if (playoutData.PlayoutId == 0)
            {
                throw new ArgumentException("Error: InsertRplData called with a PlayoutId of 0");
            }

            _loadInfo.Add(playoutData.PlayoutId, playoutData);
        }

        public string RemoveRplData(UInt32 playoutId)
        {
            string outputMessage = "";

            bool containsKey = _loadInfo.ContainsKey(playoutId);

            if (containsKey is false)
            {
                outputMessage = "No RPL found matching PlayoutID " + playoutId;
            }
            else
            {
                _loadInfo.Remove(playoutId);
                outputMessage = "RPL matching PlayoutID " + playoutId + " found and removed from memory";
                if (_currentPlayoutId == playoutId)
                {
                    _currentPlayoutId = 0;
                }
            }

            return outputMessage;
        }

        public string SetCurrentPlayout(UInt32 playoutId)
        {
            string outputMessage = "";

            bool containsKey = _loadInfo.ContainsKey(playoutId);

            if (containsKey is false)
            {
                outputMessage = "No RPL found matching PlayoutID " + playoutId;
            }
            else
            {
                _currentPlayoutId = playoutId;
                outputMessage = "RPL matching PlayoutID " + playoutId + " found.  Setting this as current PlayoutId";
            }

            return outputMessage;
        }

        public UInt32 GetCurrentPlayout()
        {
            return _currentPlayoutId;
        }

        /// <summary>
        /// I envision that this method would be used as part of the STOP command action
        /// </summary>
        public void ClearCurrentPlayout()
        {
            _currentPlayoutId = 0;
        }

        public bool IsPlayoutSelected
        {
            get
            {
                if (_currentPlayoutId == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public int LoadCount
        {
            get
            {
                return _loadInfo.Count;
            }
        }

        public RplPlayoutData GetPlayoutData()
        {
            if (_currentPlayoutId == 0)
            {
                throw new InvalidOperationException("Cannot perform GetCurrentPlayoutData when the current PlayoutId has not been set");
            }

            return _loadInfo[_currentPlayoutId];
        }

        public string GetRplLoadList()
        {
            string outputMessage = "Playout ID, Resource URL\r\n";

            foreach (RplPlayoutData data in _loadInfo.Values)
            {
                outputMessage = outputMessage + data.PlayoutId + ", " + data.ResourceUrl + "\r\n"; 
            }

            return outputMessage;
        }
    }
}

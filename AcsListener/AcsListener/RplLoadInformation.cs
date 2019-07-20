using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    /// <summary>
    /// RplLoadInformation represents the essential data that needs to be kept (static) about the RPLs that have been loaded in 
    /// to memory in AcsListener, and presumably loaded in to the ACS.
    /// </summary>
    class RplLoadInformation
    {
        private Dictionary<UInt32, RplPlayoutData> _loadInfo = new Dictionary<UInt32, RplPlayoutData>();
        private UInt32 _currentPlayoutId;

        /// <summary>
        /// Basic constructor for the RplLoadInformation object
        /// </summary>
        public RplLoadInformation()
        {
            InitializeData();
        }

        /// <summary>
        /// InitializeData is responsible for initializing (setting to 0) the current PlayoutId 
        /// </summary>
        private void InitializeData()
        {
            _currentPlayoutId = 0;
        }

        /// <summary>
        /// InsertRplData method performs a dictionary add of the PlayoutID (as the key) and the rest of the Playout data
        /// </summary>
        /// <param name="playoutData">RplPlayoutData object representing the RPL data that needs to be kept</param>
        public void InsertRplData(RplPlayoutData playoutData)
        {
            if (playoutData.PlayoutId == 0)
            {
                throw new ArgumentException("Error: InsertRplData called with a PlayoutId of 0");
            }

            _loadInfo.Add(playoutData.PlayoutId, playoutData);
        }

        /// <summary>
        /// RemoveRplData removes one stored RPL from the RplLoadInformation object
        /// </summary>
        /// <param name="playoutId">Playout ID that is to be removed</param>
        /// <returns>String containing result of the operation</returns>
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

        /// <summary>
        /// SetCurrentPlayout sets the current PlayoutId if the parameter matches one of the keys in the dictionary
        /// </summary>
        /// <param name="playoutId">Playout ID to be selected</param>
        /// <returns>String containing the result of the operation</returns>
        public string SetCurrentPlayout(UInt32 playoutId)
        {
            string outputMessage = "";

            bool containsKey = _loadInfo.ContainsKey(playoutId);

            if (containsKey is false)
            {
                outputMessage = "No RPL found matching PlayoutID " + playoutId;
                throw new ArgumentException(outputMessage);  // Get us back to the CommandProcess section where we can handle this
            }
            else
            {
                _currentPlayoutId = playoutId;
                outputMessage = "RPL matching PlayoutID " + playoutId + " found.  Setting this as current PlayoutId";
            }

            return outputMessage;
        }

        /// <summary>
        /// GetCurrentPlayout returns the currently selected Playout ID
        /// </summary>
        /// <returns>UInt32 containing the Playout ID that is currently selected</returns>
        public UInt32 GetCurrentPlayout()
        {
            return _currentPlayoutId;
        }

        /// <summary>
        /// ClearCurrentPlayout initializes the current Playout ID to 0
        /// </summary>
        public void ClearCurrentPlayout()
        {
            _currentPlayoutId = 0;
        }

        /// <summary>
        /// IsPlayoutSelected property that represents whether there is a Playout ID selected (value greater than 0)
        /// </summary>
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

        /// <summary>
        /// LoadCount property that represents the total number of RPLs currently loaded in the RplLoadInformation object
        /// </summary>
        public int LoadCount
        {
            get
            {
                return _loadInfo.Count;
            }
        }

        /// <summary>
        /// GetPlayoutData returns the RplPlayoutData object of the currently selected Playout ID from the dictionary
        /// </summary>
        /// <returns>RplPlayoutData object of the currently selected Playout ID</returns>
        public RplPlayoutData GetPlayoutData()
        {
            if (_currentPlayoutId == 0)
            {
                throw new InvalidOperationException("Cannot perform GetCurrentPlayoutData when the current PlayoutId has not been set");
            }

            return _loadInfo[_currentPlayoutId];
        }

        /// <summary>
        /// GetRplLoadList builds a list of each RPL item in the dictionary
        /// </summary>
        /// <returns>String containing the Playout ID and Resource URL for each RPL in the dictionary</returns>
        public string GetRplLoadList()
        {
            string outputMessage = "Playout ID, Resource URL\r\n";

            foreach (RplPlayoutData data in _loadInfo.Values)
            {
                outputMessage = outputMessage + data.PlayoutId + ", " + data.ResourceUrl + "\r\n"; 
            }

            return outputMessage;
        }

        public List<string> GetRplUrlList()
        {
            List<string> UrlList = new List<string>();

            foreach (RplPlayoutData data in _loadInfo.Values)
            {
                UrlList.Add(data.ResourceUrl);
            }

            return UrlList;
        }
    }
}

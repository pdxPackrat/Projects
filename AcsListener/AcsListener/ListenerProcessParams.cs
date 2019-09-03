using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using SharedCommon;

namespace AcsListener
{
    public class ListenerProcessParams
    {
        private readonly TcpClient _client;
        private readonly String _urlPath = "";
        private readonly String _timeOffset = "0";

        public ListenerProcessParams(TcpClient inputClient)
        {
            if (inputClient is null)
            {
                throw new ArgumentNullException("inputClient","Error: inputClient cannot be NULL");
            }

            this._client = inputClient;
        }

        public ListenerProcessParams(TcpClient inputClient, String inputUrlPath)
        {
            if (inputClient is null)
            {
                throw new ArgumentNullException("inputClient", "Error: inputClient cannot be NULL");
            }

            if (inputUrlPath is null)
            {
                throw new ArgumentNullException("inputUrlPath", "Error: inputUrlPath cannot be NULL");
            }

            this._client = inputClient;
            this._urlPath = inputUrlPath;
        }

        public ListenerProcessParams(TcpClient inputClient, String inputUrlPath, String inputTimeOffset)
        {
            if (inputClient is null)
            {
                throw new ArgumentNullException("inputClient", "Error: inputClient cannot be NULL");
            }

            if (inputUrlPath is null)
            {
                throw new ArgumentNullException("inputUrlPath", "Error: inputUrlPath cannot be NULL");
            }

            if (inputTimeOffset is null)
            {
                throw new ArgumentNullException("inputTimeOffset", "Error: inputTimelineOffset cannot be NULL");
            }

            this._client = inputClient;
            this._urlPath = inputUrlPath;
            this._timeOffset = inputTimeOffset;
        }

        public TcpClient Client
        {
            get
            {
                return _client;
            }
        }

        public String UrlPath
        {
            get
            {
                return _urlPath;
            }
        }

        public String TimeOffset
        {
            get
            {
                return _timeOffset;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcsConnectedEventArgs : EventArgs
    {
        public IPAddress RemoteAddress { get; set; }
    }
}
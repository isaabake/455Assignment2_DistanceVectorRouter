﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DistanceVectorRouter
{
    class Route
    {
        public string Destination;
        public string Next;
        public int Cost;
    }
}

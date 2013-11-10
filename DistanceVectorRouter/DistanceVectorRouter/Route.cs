using System;
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
        public int Cost;
        public int SourcePort;
        public int DestPort;
        public Socket ReadSocket;
        public Socket WriteSocket;
    }
}

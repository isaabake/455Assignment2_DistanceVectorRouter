using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DistanceVectorRouter
{
    class Router
    {
        RouterConfig Config;
        List<RouterConfig> RouterConfigs = new List<RouterConfig>();
        List<Route> RoutingTable = new List<Route>();

        public Router(string configDir, string routerName)
        {
            InitializeConfig(routerName, configDir);
            LoadRoutingTable(configDir);
            OpenSockets();
        }

        public void Run()
        {

        }

        private List<Socket> ReadSockets
        {
            get
            {
                return RoutingTable.Select(s => s.ReadSocket).ToList();
            }
        }

        private List<Socket> WriteSockets
        {
            get
            {
                return RoutingTable.Select(s => s.WriteSocket).ToList();
            }
        }

        private void LoadRoutingTable(string dir)
        {
            using (StreamReader RoutingTableFile = new StreamReader(Path.Combine(new string[2] { dir, this.Config.name + ".cfg" })))
            {
                while (!RoutingTableFile.EndOfStream)
                {
                    string[] line = RoutingTableFile.ReadLine().Split(' ');
                    if (line.Count() == 4)
                    {
                        RoutingTable.Add(new Route
                        {
                            Destination = line[0],
                            Cost = int.Parse(line[1]),
                            SourcePort = this.Config.port + int.Parse(line[2]),
                            DestPort = RouterConfigs.First(r => r.name == line[0]).port + int.Parse(line[3])
                        });
                    }


                }
            }

        }

        private bool InitializeConfig(string name, string dir)
        {
            using (StreamReader RouterFile = new StreamReader(Path.Combine(new string[2] { dir, "routers" })))
            {
                while (!RouterFile.EndOfStream)
                {
                    string[] line = RouterFile.ReadLine().Split(' ');
                    if (line.Count() == 3)
                    {
                        RouterConfigs.Add(new RouterConfig { name = line[0], hostname = line[1], port = int.Parse(line[2]) });
                    }
                }
            }

            this.Config = RouterConfigs.Where(config => config.name == name).FirstOrDefault();
            if (this.Config != null)
            {
                return true;
            }
            return false;
        }

        private void OpenSockets()
        {
            // Creates one SocketPermission object for access restrictions
            SocketPermission permission = new SocketPermission(
            NetworkAccess.Accept,     // Allowed to accept connections 
            TransportType.Udp,        // Defines transport types 
            "",                       // The IP addresses of local host 
            SocketPermission.AllPorts // Specifies all ports 
            );

            // Ensures the code to have permission to access a Socket 
            permission.Demand();


            foreach (Route route in RoutingTable)
            {  
                //////////
                ///Read///
                //////////
                // Resolves a host name to an IPHostEntry instance 
                IPHostEntry srcIpHost = Dns.GetHostEntry(this.Config.hostname);

                //Get the first IP Address
                IPAddress srcIpAddr = srcIpHost.AddressList[0];

                // Creates a network endpoint 
                IPEndPoint srcIpEndPoint = new IPEndPoint(srcIpAddr, route.SourcePort);

                // Create the socket and bind
                route.ReadSocket = new Socket(srcIpAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                route.ReadSocket.Bind(srcIpEndPoint);



                ///////////
                ///Write///
                ///////////
                // Resolves a host name to an IPHostEntry instance 
                IPHostEntry destIpHost = Dns.GetHostEntry(RouterConfigs.Where(config => config.name == route.Destination).First().hostname);

                // Get the first IP address
                IPAddress destIpAddr = destIpHost.AddressList[0];

                // Creates a network endpoint 
                IPEndPoint destIpEndPoint = new IPEndPoint(destIpAddr, route.DestPort);

                //Create the socket and bind
                route.WriteSocket = new Socket(destIpAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                route.WriteSocket.Bind(destIpEndPoint);
            }
        }
    }
}

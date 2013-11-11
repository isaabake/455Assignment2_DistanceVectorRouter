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
        public static int INF = 64;
        RouterConfig Config;
        List<RouterConfig> RouterConfigs = new List<RouterConfig>();
        List<Route> RoutingTable = new List<Route>();
        List<Link> NeighborSockets = new List<Link>();


        public Router(string configDir, string routerName)
        {
            InitializeConfig(routerName, configDir);
            LoadRoutingTable(configDir);
            PrintRoutingTable("P");
        }

        public void Run()
        {
            while (true)
            {
                List<Socket> ReadSockets = NeighborSockets.Select(neighbor => neighbor.Socket).ToList();
                List<Socket> WriteSockets = NeighborSockets.Select(neighbor => neighbor.Socket).ToList();
                Socket.Select(ReadSockets, WriteSockets, null, 1000);
                foreach (Socket sock in ReadSockets)
                {
                    byte[] buf = new byte[1024];
                    sock.Receive(buf);

                    switch (buf.ToString()[0])
                    {
                        case 'U':
                            UpdateRoutingTable(buf.ToString(), NeighborSockets.Where(neighbor => neighbor.Socket == sock).FirstOrDefault());
                            break;

                        case 'P':
                            PrintRoutingTable(buf.ToString());
                            break;

                        case 'L':
                            UpdateLinkCost(buf.ToString());
                            break;
                    }





                }
            }
        }

        private void UpdateLinkCost(string message)
        {

        }

        private void PrintRoutingTable(string message)
        {
            if (message != null)
            {
                string[] args = message.Split(' ');
                if (args.Count() == 2)
                {
                    Route route = RoutingTable.Where(r => r.Destination == args[1]).FirstOrDefault();
                    if (route != null)
                    {
                        Console.WriteLine("Destination={0}, Next={1}, Cost={2}", route.Destination, route.Next, route.Cost);
                        return;
                    }
                }
            }
            foreach (Route route in RoutingTable)
            {
                Console.WriteLine("Destination={0}, Next={1}, Cost={2}", route.Destination, route.Next, route.Cost);
            }
            return;
        }

        /// <summary>
        /// Updates the routing table based on a message in form "U d cost"
        /// Where d is the destination node
        /// </summary>
        /// <param name="message">The message received on the socket</param>
        /// <param name="from">The Link where the message was received</param>
        private void UpdateRoutingTable(string message, Link from)
        {
            string[] updateArgs = message.Split(' ');

            //Route where destination == d and next hop == from
            Route updateRoute = RoutingTable.Where(route => route.Destination == updateArgs[1] && route.Next == from.Name).FirstOrDefault();
            if (updateRoute != null)
            {
                //Try converting cost value to int
                if (int.TryParse(updateArgs[2], out updateRoute.Cost))
                {
                    //Print out the change
                    Console.WriteLine("Updated route from {0}: Destination={1}, Next={2}, Cost={3}", from.Name, updateRoute.Destination, updateRoute.Next, updateRoute.Cost);
                    DistributeRoutingTable();
                }
                
            }
        }

        private void DistributeRoutingTable()
        {

        }

        private void LoadRoutingTable(string dir)
        {
            //Initialize Routing Table for all routers to infinity
            foreach (RouterConfig conf in RouterConfigs)
            {
                RoutingTable.Add(new Route
                {
                    Destination = conf.name,
                    Next = "",
                    Cost = INF
                });
            }

            //Open Routing Table File for the router and update the routing table entries. Also open ports
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
                            Next = line[0],
                            Cost = int.Parse(line[1])
                        });

                        RouterConfig remoteRouterConf = RouterConfigs.Where(conf => conf.name == line[0]).First();
                        NeighborSockets.Add(new Link
                        {
                            Name = line[0],
                            Socket = OpenSocket
                            (
                                this.Config.hostname,                       //local hostname
                                int.Parse(line[2]) + this.Config.port,      //local port
                                remoteRouterConf.hostname,                  //Remote hostname
                                int.Parse(line[3]) + remoteRouterConf.port  //Remote port
                            )
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

        /// <summary>
        /// Opens a UDP socket with specified hostname and port information
        /// </summary>
        /// <param name="localhostname">local hostname</param>
        /// <param name="localport">local port number</param>
        /// <param name="remotehostname">remote hostname</param>
        /// <param name="remoteport">remote port</param>
        /// <returns></returns>
        private Socket OpenSocket(string localhostname, int localport, string remotehostname, int remoteport)
        {
            // Get the local IP Address
            IPAddress localIpAddr = Dns.GetHostAddresses(localhostname)[0];

            // Create a network endpoint 
            IPEndPoint localIpEndPoint = new IPEndPoint(localIpAddr, localport);

            // Create the socket and bind locally
            Socket sock = new Socket(localIpAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(localIpEndPoint);

            //Get the remote IP Address
            IPAddress remoteIpAddr = Dns.GetHostAddresses(remotehostname)[0];

            // Create a network endpoint
            IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIpAddr, remoteport);

            //Connect to remote
            sock.Connect(remoteIpEndPoint);

            return sock;
        }
    }
}

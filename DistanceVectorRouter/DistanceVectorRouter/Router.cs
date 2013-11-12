using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;
using System.Diagnostics;

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
            NeighborSockets.Add(new Link { Name = this.Config.name, Socket = OpenSocket(this.Config.hostname, this.Config.port, null, 0) });    //Add local port listen
            Console.WriteLine("Router {0}", routerName);
        }

        public void Run()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                List<Socket> ReadSockets = NeighborSockets.Select(neighbor => neighbor.Socket).ToList();
                Socket.Select(ReadSockets, null, null, 10000);  //Timeout = 10ms
                foreach (Socket sock in ReadSockets)
                {
                    byte[] buf = new byte[1024];
                    sock.Receive(buf);
                    string message = System.Text.Encoding.Default.GetString(buf);

                    switch (message[0])
                    {
                        case 'U':
                            UpdateRoutingTable(message, NeighborSockets.Where(neighbor => neighbor.Socket == sock).FirstOrDefault());
                            break;

                        case 'P':
                            PrintRoutingTable(message);
                            break;

                        case 'L':
                            UpdateLinkCost(message);
                            break;
                    }
                }
                if (watch.Elapsed.Seconds > 10)
                {
                    watch.Reset();
                    IEnumerable<string> Neighbors = NeighborSockets.Select(s => s.Name).Where(s => s != this.Config.name);
                    foreach (Route route in RoutingTable.Where(r => r.Cost < INF && Neighbors.Contains(r.Destination)))
                    {
                        DistributeUpdatedRoute(route);
                    }
                    watch.Start();

                }
            }
        }

        private void UpdateLinkCost(string message)
        {
            if (message != null)
            {
                string[] args = message.Split(' ');
                if (args.Count() == 3)
                {
                    string neighbor = args[1];
                    int newCost;
                    if (!int.TryParse(args[2], out newCost))
                    {
                        Console.WriteLine("Error in Link update message: {0}", message);
                        return;
                    }

                    foreach (Route route in RoutingTable.Where(r => r.Next == neighbor))
                    {
                        Route neighborRoute = RoutingTable.Where(r => r.Destination == neighbor).FirstOrDefault();
                        if (neighborRoute == null)
                        {
                            Console.WriteLine("Error in Link update: No neighbor matching {0}", neighbor);
                            return;
                        }
                        Console.WriteLine("Link update");
                        Console.Write("Old values: ");
                        PrintRoute(route);
                        route.Cost = route.Cost - neighborRoute.Cost + newCost;
                        Console.Write("New values: ");
                        PrintRoute(route);
                        DistributeUpdatedRoute(route);   //Send updated routing table to neighbors
                    }
                }
                
            }
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
                        PrintRoute(route);
                        return;
                    }
                }
            }
            foreach (Route route in RoutingTable)
            {
                PrintRoute(route);
            }
            return;
        }

        private void PrintRoute(Route route)
        {
            Console.WriteLine("Destination={0}, Next={1}, Cost={2}", route.Destination, route.Next, route.Cost);
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
            Route updateRoute = RoutingTable.Where(route => route.Destination == updateArgs[1]).FirstOrDefault();
            if (updateRoute != null)
            {
                int costArg;

                //Try converting cost value to int
                if (int.TryParse(updateArgs[2], out costArg))
                {
                    Route neighbor = RoutingTable.Where(r => r.Next == from.Name && r.Destination == from.Name).FirstOrDefault(); //Get the route to get to the next hop
                    if (neighbor == null)
                    {
                        //Console.WriteLine("Error in UpdateRoutingTable: No neighbor for next hop to: {0}", from.Name);
                        return;
                    }
                    int newCost = costArg + neighbor.Cost;    //newCost is cost to get to the next hop + the update

                    if (updateRoute.Next != from.Name && newCost < updateRoute.Cost)    //If it's not from the current next but has lower cost, update route to use that node instead 
                    {
                        updateRoute.Cost = newCost;
                        updateRoute.Next = from.Name;

                        //Print out the change
                        Console.Write("Updated route from {0}: ", from.Name);
                        PrintRoute(updateRoute);
                        DistributeUpdatedRoute(updateRoute);
                    }
                    else if (updateRoute.Next == from.Name && updateRoute.Cost != newCost)     //Else it's not from a different router, update the table if the cost is different
                    {
                        updateRoute.Cost = newCost;

                        //Print out the change
                        Console.WriteLine("Updated route from {0}: Destination={1}, Next={2}, Cost={3}", from.Name, updateRoute.Destination, updateRoute.Next, updateRoute.Cost);
                        DistributeUpdatedRoute(updateRoute);
                    }
                }
            }
        }

        private void DistributeUpdatedRoute(Route route)
        {
            if (route.Destination != this.Config.name)  //Not route to get to yourself...
            {
                //Don't send to yourself and dont send updates for a router to that router (sending update b to b)
                foreach (Link link in NeighborSockets.Where(l => l.Name != this.Config.name &&
                                                                 l.Name != route.Destination &&
                                                                 l.Name != route.Next))  
                {
                    Console.WriteLine("Sending update for destination {0} to {1}", route.Destination, link.Name);
                    byte[] message = System.Text.Encoding.Default.GetBytes(string.Format("U {0} {1}", route.Destination, route.Cost));
                    Console.WriteLine("!debug! sending \"{0}\" to {1}", Encoding.Default.GetString(message), link.Name);
                    int sent = link.Socket.Send(message);
                    Console.WriteLine("Send returned: {0}", sent);
                }
            }
        }

        private void LoadRoutingTable(string dir)
        {
            //Initialize Routing Table for all routers to infinity
            foreach (RouterConfig conf in RouterConfigs.Where(r => r.name != this.Config.name))
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
                        Route route = RoutingTable.Where(r => r.Destination == line[0]).FirstOrDefault();
                        if (route != null)
                        {
                            route.Next = line[0];
                            route.Cost = int.Parse(line[1]);
                        }
                        else
                        {
                            RoutingTable.Add(new Route
                            {
                                Destination = line[0],
                                Next = line[0],
                                Cost = int.Parse(line[1])
                            });
                        }

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
            IPAddress localIpAddr = Dns.GetHostAddresses(localhostname).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

            // Create a network endpoint 
            IPEndPoint localIpEndPoint = new IPEndPoint(localIpAddr, localport);

            // Create the socket and bind locally
            Socket sock = new Socket(localIpAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(localIpEndPoint);

            if (remotehostname != null && remoteport != 0)
            {
                //Get the remote IP Address
                IPAddress remoteIpAddr = Dns.GetHostAddresses(remotehostname).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

                // Create a network endpoint
                IPEndPoint remoteIpEndPoint = new IPEndPoint(remoteIpAddr, remoteport);

                //Connect to remote
                sock.Connect(remoteIpEndPoint);
            }

            return sock;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DistanceVectorRouter
{
    class Router
    {
        RouterConfig Config;

        public Router(string name)
        {
            LoadRouterConfig(name);
        }

        public void Run()
        {

        }

        private void MakeRouterConfigs()
        {
            XmlSerializer RouterConfigWriter = new XmlSerializer(typeof(List<RouterConfig>));
            StreamWriter RouterConfigFile = new StreamWriter(@"routers.xml");

            List<RouterConfig> RouterConfigs = new List<RouterConfig>();

            RouterConfigs.Add(new RouterConfig() { name = "A", hostname = "localhost", port = 20000 });
            RouterConfigs.Add(new RouterConfig() { name = "B", hostname = "localhost", port = 20010 });
            RouterConfigs.Add(new RouterConfig() { name = "C", hostname = "localhost", port = 20020 });
            RouterConfigs.Add(new RouterConfig() { name = "D", hostname = "localhost", port = 20030 });
            RouterConfigs.Add(new RouterConfig() { name = "E", hostname = "localhost", port = 20040 });

            RouterConfigWriter.Serialize(RouterConfigFile, RouterConfigs);
            RouterConfigFile.Close();
        }

        private void MakeLinks()
        {
            XmlSerializer LinkWriter = new XmlSerializer(typeof(List<Link>));
            StreamWriter LinkFile = new StreamWriter(@"links.xml");

            List<Link> Links = new List<Link>();

            Links.Add(new Link() { Source = "A", Destination = "B", Cost = 7 });
            Links.Add(new Link() { Source = "B", Destination = "C", Cost = 1 });
            Links.Add(new Link() { Source = "C", Destination = "D", Cost = 2 });
            Links.Add(new Link() { Source = "D", Destination = "E", Cost = 2 });
            Links.Add(new Link() { Source = "E", Destination = "A", Cost = 1 });
            Links.Add(new Link() { Source = "E", Destination = "B", Cost = 1 });

            LinkWriter.Serialize(LinkFile, Links);
            LinkFile.Close();
        }

        private bool LoadRouterConfig(string name)
        {
            XmlSerializer RouterConfigReader = new XmlSerializer(typeof(List<RouterConfig>));
            StreamReader RouterConfigFile = new StreamReader(@"routers.xml");

            //Do not catch exception, let it go up
            List<RouterConfig> RouterConfigs = (List<RouterConfig>)RouterConfigReader.Deserialize(RouterConfigFile);

            RouterConfigFile.Close();
            
            this.Config = RouterConfigs.Where(config => config.name == name).FirstOrDefault();
            if (this.Config != null)
            {
                return true;
            }
            return false;
        }
    }
}

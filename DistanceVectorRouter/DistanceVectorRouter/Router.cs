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

        public Router()
        {
            
        }

        public void Run()
        {

        }

        /// <summary>
        /// Initializes the router from the router config file
        /// </summary>
        /// <param name="configfile"></param>
        /// <param name="name"></param>
        public void Initialize(string configfile, string name)
        {
            if (!LoadRouterConfig(name))
            {

            }
        }

        private void MakeRouterConfigs()
        {
            XmlSerializer RouterConfigWriter = new XmlSerializer(typeof(List<RouterConfig>));
            StreamWriter RouterConfigFile = new StreamWriter(@"routerconfig.xml");

            List<RouterConfig> RouterConfigs = new List<RouterConfig>();

            RouterConfigs.Add(new RouterConfig() { name = "A", hostname = "localhost", port = 20000 });
            RouterConfigs.Add(new RouterConfig() { name = "B", hostname = "localhost", port = 20010 });
            RouterConfigs.Add(new RouterConfig() { name = "C", hostname = "localhost", port = 20020 });
            RouterConfigs.Add(new RouterConfig() { name = "D", hostname = "localhost", port = 20030 });
            RouterConfigs.Add(new RouterConfig() { name = "E", hostname = "localhost", port = 20040 });

            RouterConfigWriter.Serialize(RouterConfigFile, RouterConfigs);
            RouterConfigFile.Close();
        }

        private bool LoadRouterConfig(string name)
        {
            XmlSerializer RouterConfigReader = new XmlSerializer(typeof(List<RouterConfig>));
            StreamReader RouterConfigFile = new StreamReader(@"routerconfig.xml");
            List<RouterConfig> RouterConfigs;

            try
            {
                RouterConfigs = (List<RouterConfig>)RouterConfigReader.Deserialize(RouterConfigFile);
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
            
            this.Config = RouterConfigs.Where(config => config.name == name).FirstOrDefault();
            if (this.Config != null)
            {
                return true;
            }
            return false;
        }
    }
}

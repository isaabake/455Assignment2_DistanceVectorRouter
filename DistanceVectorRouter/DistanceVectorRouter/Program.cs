﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistanceVectorRouter
{
    class Program
    {
        static void Main(string[] args)
        {
            Router router = new Router(args[0], args[1]);
            router.Run();
        }
    }
}

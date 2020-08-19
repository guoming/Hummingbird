using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.Canal.Subscripters
{
    public class ConsoleSubscripter : ISubscripter
    {
        public bool Process(CanalEventEntry[] entrys)
        {
            foreach(var entry in entrys)
            {
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(entry));
            }

            return true;
        }
    }
}

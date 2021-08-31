using System;
using System.Collections.Generic;

namespace Hummingbird.Example.Events.MongoShark
{


    public class MongodbSharkEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public long ts { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long h { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public long v { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string op { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ns { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<OItem> o { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public O2 o2 { get; set; }

        public class OItem
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public dynamic Value { get; set; }
        }

        public class O2
        {
            /// <summary>
            /// 
            /// </summary>
            public string _id { get; set; }
        }
    }

   

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using couchbase_net_rest;

namespace TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {

            //var config = new CouchbaseRestConfiguration("default", "guest", "guest",
            //                                            new Uri("http://server1:8091"),
            //                                            new Uri("http://server2:8091"),
            //                                            new Uri("http://server3:8091")) { RequestTimeout = TimeSpan.FromMilliseconds(2000) };

            //var client = new CouchbaseClient(config);

            //var item = client.GetView<JObject>("ViewGroup", "ViewName")
            //                                .Key(1000030760)
            //                                .Stale(ViewStaleType.StaleOk)
            //                                .IncludeDocs(true)
            //                                .Limit(1)
            //                                .Query()
            //                                .FirstOrDefault();

            //var item2 = client.Cache().GetJson<JObject>("key");

        }
    }
}

couchbase-net-rest
=========

Non-official couchbase driver which uses a light weight rest query engine to call couchbase views instead of the heavier TCP consistent socket official driver.

License
-----------
Copyright 2014, James Roland under Apache License, V2.0. See LICENSE file.

Summary
-----------
The idea behind this project is to have a simpler driver which uses the Couchbase Rest API to query views instead of using consistent TCP sockets.  To achieve this the driver will do the following:
* Query the couchbase pools to create a collection of active nodes
* Query the couchbase pools on a schedule to add new nodes or remove unhealthy nodes
* Nodes that have a certain fail rate should automatically get removed from collection
* Use a selector function to select which node to query (default round robin)
* Imitate the official driver as close as possible for compatibility

Example Query
-----------
```sh
//create configuration 
var config = new CouchbaseRestConfiguration("default", "guest", "guest",
        new Uri("http://server1:8091"),
        new Uri("http://server2:8091"),
        new Uri("http://server3:8091")) { RequestTimeout = TimeSpan.FromMilliseconds(2000) };


//initialize a new client
var client = new CouchbaseClient(config);

//call a view
var item = client.GetView<JObject>("ViewGroup", "ViewName")
                                .Key(1000030760)
                                .Stale(ViewStaleType.StaleOk)
                                .IncludeDocs(true)
                                .Limit(1)
                                .Query()
                                .FirstOrDefault();

//execute direct get with a key
var item2 = client.Cache().GetJson<JObject>("key");

```

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Enyim.Caching;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using couchbase_net_rest.Models;

namespace couchbase_net_rest
{
    public class ConnectionPool : IDisposable
    {
        private readonly CouchbaseRestConfiguration _config;
        private readonly ConcurrentDictionary<int, CouchbaseNode> _nodes = new ConcurrentDictionary<int, CouchbaseNode>();
        private readonly IScheduledTimer _poolStatusCheckSchedule;
        private readonly Random _random = new Random(DateTime.Now.Millisecond);
        private MemcachedClient _memcachedClient;

        public MemcachedClient MemcachedClient
        {
            get
            {
                while (_nodes.Count <= 0)
                {
                    _config.Log.WarnFormat("Couchbase has no nodes in the pool.  Blocking until nodes come online.");
                    Thread.Sleep(200);
                }
                return _memcachedClient;
            }
        }

        public ConnectionPool(CouchbaseRestConfiguration config)
        {
            if (config == null) throw new ArgumentNullException("config");

            _config = config;

            _poolStatusCheckSchedule = new ScheduledTimer()
                .Do(CheckPoolStatus)
                .Every(TimeSpan.FromSeconds(_config.PollCouchbasePoolStatusSeconds))
                .StartingAt(DateTime.Now)
                .Begin();
        }

        public IEnumerable<T> ExecuteQueryView<T>(IViewQuery view)
        {
            for (int i = 0; i < view.RetriesOnFailer; i++)
            {
                var node = SelectNode();
                var query = node.GetQuery().Merge(view.Request).Address;
                try
                {
                    _config.Log.DebugFormat("Executing view query: {0}", query);
                    return GetResult<T>(query, _config.Username, _config.Password);
                }
                catch (NodeOfflineException ex)
                {
                    _config.Log.ErrorFormat("Failed view query: {0}", query);
                    node.IncrementError();
                }
                catch (Exception ex)
                {
                    _config.Log.WarnFormat("The following query failed: {0}. RetryAttemopt={1} Exception={2}", query, i, ex.Message);
                }
            }

            throw new CouchbaseQueryFailedException(
                "Failed to execute view query after {0} retries.  Query attempted: {1}", view.RetriesOnFailer, view.Request);
        }

        private CouchbaseNode SelectNode()
        {
            while (_nodes.Count <= 0)
            {
                _config.Log.WarnFormat("Couchbase has no nodes in the pool.  Blocking until nodes come online.");
                Thread.Sleep(200);
            }
            var index = _random.Next(0, _nodes.Count);
            return _nodes.ElementAt(index).Value;
        }

        private IEnumerable<T> GetResult<T>(Uri restQuery, string username, string password)
        {
            var result = ExecuteQuery<JObject>(restQuery, username, password);

            return result.SelectToken("rows")
                .Children()
                .Select(x =>
                {
                    var doc = x.SelectToken("doc");
                    if (doc != null)
                    {
                        var json = doc.SelectToken("json");
                        return json.ToObject<T>();
                    }

                    return x.ToObject<T>();
                });
        }

        private T ExecuteQuery<T>(Uri restQuery, string username, string password)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(restQuery);
            httpWebRequest.Proxy = null;
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "GET";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            httpWebRequest.Credentials = new NetworkCredential(username, password);
            httpWebRequest.Timeout = (int)_config.RequestTimeout.TotalMilliseconds;

            try
            {
                using (var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (httpResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new NodeOfflineException("Failed to contact node at:{0}", restQuery);
                    }

                    var responseStream = httpResponse.GetResponseStream();
                    if (responseStream == null)
                    {
                        throw new NodeOfflineException("Node failed to return response at:{0}", restQuery);
                    }


                    var serializer = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
                    using (TextReader stream = new StreamReader(responseStream))
                    using (JsonReader reader = new JsonTextReader(stream))
                    {
                        return serializer.Deserialize<T>(reader);
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Message == "Unable to connect to the remote server")
                    throw new NodeOfflineException("{0}.  Query:{1}", ex.Message, restQuery);
                throw;
            }
        }

        private void CheckPoolStatus()
        {
            foreach (var uri in _config.CouchbaseServers)
            {
                try
                {
                    _config.Log.InfoFormat("Attempting to load pool metadata from : {0}", uri);
                    var builder = new UrlBuilder(uri);
                    var query = builder.Clone().AddCommand("pools", _config.Bucket);
                    var pools = ExecuteQuery<JObject>(query.Address, _config.Username, _config.Password);

                    if (pools == null) throw new BucketNotAvailableException("No node data for bucket: {0}", query);

                    InitializeActiveNodes(pools, query);

                    RemoveFailedNodes(pools);

                    break; //found nodes so break out
                }
                catch (Exception ex)
                {
                    _config.Log.WarnFormat("Failed to load pool metadata from: {0}.  Exception={1}", uri, ex);
                }
            }

            if (_nodes.Count <= 0) _config.Log.FatalFormat("Failed to load pool metadata from any node.  Unable to configure client.");
        }

        private void InitializeActiveNodes(JObject pools, IUrlBuilder query)
        {
            var activeNodes = (from n in pools["nodes"]
                               where n.Value<string>("clusterMembership") == "active" && n.Value<string>("status") == "healthy"
                               select new Uri(n.Value<string>("couchApiBase"))).ToList();

            if (activeNodes.Count <= 0) throw new BucketNotAvailableException("No node data for bucket: {0}", query);


            var addCount = 0;
            foreach (var node in activeNodes.Select(x => new CouchbaseNode(_config, x)))
            {
                var closureNode = node;
                closureNode.OnNodeOffline += OnNodeFailedEvent;
                _nodes.AddOrUpdate(node.Id, i =>
                {
                    _config.Log.InfoFormat("Adding node to pool:{0}", closureNode.Uri);
                    addCount++;
                    return closureNode;
                }, (i, cnode) => cnode);
            }

            if (addCount > 0)
            {
                OnNodesAddedEvent();
            }
        }

        private void RemoveFailedNodes(JObject pools)
        {
            var failedNodes = (from n in pools["nodes"]
                               where n.Value<string>("clusterMembership") != "active" || n.Value<string>("status") != "healthy"
                               select new Uri(n.Value<string>("couchApiBase"))).ToList();

            foreach (var node in failedNodes.Select(x => new CouchbaseNode(_config, x)))
            {
                OnNodeFailedEvent(node);
            }
        }

        private void OnNodesAddedEvent()
        {
            InitializeMemcachedNodes();
        }

        private void OnNodeFailedEvent(CouchbaseNode failedNode)
        {
            CouchbaseNode removedNode;
            if (_nodes.TryRemove(failedNode.Id, out removedNode))
            {
                _config.Log.WarnFormat("Successfully removed failed node:{0}", failedNode.Uri);
                InitializeMemcachedNodes();
            }
        }

        private void InitializeMemcachedNodes()
        {
            _config.Log.InfoFormat("Nodes modified:  Initializing new memcached client.");

            foreach (var node in _nodes.Values)
            {
                _config.MemcachedConfiguration.AddServer(node.Uri.Host, _config.MemcachedProxyPort);
            }

            var client = new MemcachedClient(_config.MemcachedConfiguration);
            //warm up client
            client.Get("");

            //swap in client
            var oldClient = _memcachedClient;
            using (oldClient)
            {
                _config.Log.InfoFormat("Swapping in new memcached client.");
                _memcachedClient = client;
            }
        }

        //private void InitializeMemcachedNodes()
        //{
        //    var activeNodes = pools["nodes"]
        //        .Where(n => n.Value<string>("clusterMembership") == "active")
        //        .Select(n =>
        //        {
        //            var host = new Uri(n.Value<string>("couchApiBase"));
        //            var port = n.SelectToken("ports.proxy").Value<int>();
        //            return new Uri(string.Format("http://{0}:{1}", host.Host, port));
        //        })
        //        .ToList();

        //    foreach (var activeNode in activeNodes)
        //    {
        //        _config.Log.InfoFormat("Adding memcache node to pool:{0}", activeNode);
        //        _config.MemcachedConfiguration.AddServer(activeNode.Host, activeNode.Port);
        //    }

        //    _memcachedClient = new MemcachedClient(_config.MemcachedConfiguration);
        //}

        public void Dispose()
        {
            using (_poolStatusCheckSchedule)
            using (_memcachedClient)
            {

            }
        }
    }
}
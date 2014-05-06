using System;
using System.Net;

namespace couchbase_net_rest.Models
{
    public class CouchbaseNode
    {
        private readonly CouchbaseRestConfiguration _config;
        public event Action<CouchbaseNode> OnNodeOffline;

        private readonly IUrlBuilder _builder;
        
        public int Id { get; private set; }
        public Uri Uri { get; private set; }
        public int ErrorCount { get; private set; }
        public DateTime ErrorAge { get; private set; }

        public CouchbaseNode(CouchbaseRestConfiguration config, Uri uri)
        {
            _config = config;
            Id = uri.GetHashCode();
            Uri = uri;
            _builder = new UrlBuilder(uri);
            ErrorAge = DateTime.UtcNow;

            //The first time a request is made to a URI, the ServicePointManager
            //will create a ServicePoint to manage connections to a particular host
            ServicePointManager.FindServicePoint(Uri).SetTcpKeepAlive(true, 300, 30);
        }

        public IUrlBuilder GetQuery()
        {
            return _builder.Clone();
        }

        public void IncrementError()
        {
            if (ErrorAge > DateTime.UtcNow.AddSeconds((-1) * _config.ResetErrorCountAfterSeconds))
            {
                _config.Log.WarnFormat("Incrementing error count for node: {0}.  Current count:{1}", Uri, ErrorCount);
                if (++ErrorCount > _config.FailNodeOnErrorCount && OnNodeOffline != null)
                {
                    _config.Log.ErrorFormat("Failing node: {0}.  Error count exceeded.", Uri);
                    OnNodeOffline(this);
                }
            }
            else
            {
                _config.Log.WarnFormat("Starting error tracking for node: {0}.", Uri);
                ErrorCount = 1;
                ErrorAge = DateTime.UtcNow;
            }
        }
    }
}

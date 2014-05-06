using Enyim.Caching;

namespace couchbase_net_rest
{
    public class CouchbaseClient : ICouchbaseClient
    {
        private readonly CouchbaseRestConfiguration _config;
        private readonly ConnectionPool _pool;

        public CouchbaseClient(CouchbaseRestConfiguration config)
        {
            _config = config;
            _pool = new ConnectionPool(_config);
        }

        public ViewQuery<T> GetView<T>(string viewGroup, string view)
        {
            return new ViewQuery<T>(_pool, _config.ClientId, _config.Bucket, viewGroup, view);
        }

        public MemcachedClient Cache()
        {
            return _pool.MemcachedClient;
        }
    }
}

using Enyim.Caching;

namespace couchbase_net_rest
{
    public interface ICouchbaseClient
    {
        ViewQuery<T> GetView<T>(string viewGroup, string view);
        MemcachedClient Cache();
    }
}
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace couchbase_net_rest
{
    public class ViewQuery<T> : IViewQuery
    {
        private readonly ConnectionPool _pool;
        private readonly IQueryBuilder _query;

        private int _skip;
        private int _limit = 10;

        public int RetriesOnFailer { get; private set; }
        public IQueryBuilder Request { get { return _query; } }

        public ViewQuery(ConnectionPool pool, string clientId, string bucket, string viewGroup, string view)
        {
            _pool = pool;
            RetriesOnFailer = 3;
            _query = new QueryBuilder(clientId)
                .AddCommand(bucket, "_design", viewGroup, "_view", view)
                .AddParameter("limit", _limit)
                .AddParameter("skip", 0);
        }

        public ViewQuery<T> Key(object key)
        {
            _query.AddParameter("key", key);
            return this;
        }

        public ViewQuery<T> Keys(params object[] keys)
        {
            var array = new JArray(keys);
            _query.AddParameter("keys", array.ToString());
            return this;
        }

        public ViewQuery<T> Descending()
        {
            _query.AddParameter("descending", true);
            return this;
        }

        public ViewQuery<T> StartKey(object key)
        {
            _query.AddParameter("startkey", key);
            return this;
        }

        public ViewQuery<T> EndKey(object key)
        {
            _query.AddParameter("endkey", key);
            return this;
        }

        public ViewQuery<T> Group(bool enable)
        {
            _query.AddParameter("group", enable);
            return this;
        }

        public ViewQuery<T> GroupLevel(int level)
        {
            _query.AddParameter("group_level", level);
            return this;
        }

        public ViewQuery<T> InclusiveEnd(bool enable)
        {
            _query.AddParameter("inclusive_end", enable);
            return this;
        }

        public ViewQuery<T> IncludeDocs(bool enable)
        {
            _query.AddParameter("include_docs", enable);
            return this;
        }

        public ViewQuery<T> Debug(bool enable)
        {
            _query.AddParameter("debug", enable);
            return this;
        }

        public ViewQuery<T> Limit(int limit)
        {
            _limit = limit;
            _query.Parameters.Set("limit", limit.ToString());
            return this;
        }

        public ViewQuery<T> Skip(int skip)
        {
            _skip = skip;
            _query.Parameters.Set("skip", skip.ToString());
            return this;
        }

        private void AddSkip(int skip)
        {
            _skip += skip;
            _query.Parameters.Set("skip", _skip.ToString());
        }

        public ViewQuery<T> Stale(ViewStaleType staleType)
        {
            switch (staleType)
            {
                case ViewStaleType.StaleOk:
                    _query.AddParameter("stale", "ok");
                    break;
                case ViewStaleType.UpdateAfter:
                    _query.AddParameter("stale", "update_after");
                    break;
                case ViewStaleType.NotStale:
                    _query.AddParameter("stale", "false");
                    break;
                default:
                    throw new NotSupportedException(string.Format("Given staleType not supported: {0}", staleType));
            }

            return this;
        }

        public ViewQuery<T> ViewWaitTimeout(TimeSpan timeout)
        {
            _query.AddParameter("connection_timeout", timeout.TotalMilliseconds);
            return this;
        }

        public ViewQuery<T> Retry(int count)
        {
            RetriesOnFailer = count;
            return this;
        }


        /// <summary>
        /// Returns IEnumerable of results found by the constructed query
        /// </summary>
        /// <param name="autoIncrementSkip">Set true to automatically return all pages until complete.</param>
        /// <returns></returns>
        public IEnumerable<T> Query(bool autoIncrementSkip = false)
        {
            do
            {
                int count = 0;
                foreach (var document in _pool.ExecuteQueryView<T>(this))
                {
                    count++;
                    yield return document;
                }

                if (count == 0) yield break;

                AddSkip(_limit);
            } while (autoIncrementSkip);
        }
    }
}
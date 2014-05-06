using System;
using System.Text;
using System.Threading.Tasks;

namespace couchbase_net_rest
{
    public class NodeOfflineException : Exception
    {
        public NodeOfflineException(string message, params object[] args)
            : base(string.Format(message, args))
        {

        }
    }

    public class BucketNotAvailableException : Exception
    {
        public BucketNotAvailableException(string message, params object[] args)
            : base(string.Format(message, args))
        {

        }
    }

    public class CouchbaseQueryFailedException : Exception
    {
        public CouchbaseQueryFailedException(string message, params object[] args)
            : base(string.Format(message, args))
        {

        }
    }
}

namespace couchbase_net_rest
{
    public interface IViewQuery
    {
        int RetriesOnFailer { get; }
        IQueryBuilder Request { get; }
    }
}
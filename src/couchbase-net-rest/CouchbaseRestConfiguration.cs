using System;
using System.Collections.Generic;
using System.Net;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace couchbase_net_rest
{
    public class CouchbaseRestConfiguration
    {
        public int PollCouchbasePoolStatusSeconds = 60;
        public int FailNodeOnErrorCount = 10;
        public int ResetErrorCountAfterSeconds = 60;
        public int MemcachedProxyPort = 11211;

        public ILog Log { get; set;}
        public List<Uri> CouchbaseServers { get; set;}
        public string ClientId { get; set; }
        public string Bucket { get; set;}
        public string Username { get; set;}
        public string Password { get; set;}
        public TimeSpan RequestTimeout { get; set;}
        public MemcachedClientConfiguration MemcachedConfiguration { get; set; }

        public CouchbaseRestConfiguration(string bucket, string username, string password, params Uri[] couchbaseServers)
        {
            if (couchbaseServers == null) throw new ArgumentNullException("couchbaseServers");
            if (bucket == null) throw new ArgumentNullException("bucket");
            if (username == null) throw new ArgumentNullException("username");
            if (password == null) throw new ArgumentNullException("password");

            CouchbaseServers =  new List<Uri>(couchbaseServers);
            Bucket = bucket;
            Username = username;
            Password = password;
            ClientId = "couchbase_net_rest";

            RequestTimeout = TimeSpan.FromMilliseconds(500);

            log4net.Config.XmlConfigurator.Configure();

            Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            MemcachedConfiguration = new MemcachedClientConfiguration();
            MemcachedConfiguration.Protocol = MemcachedProtocol.Binary;
            MemcachedConfiguration.Authentication.Type = typeof(PlainTextAuthenticator);
            MemcachedConfiguration.Authentication.Parameters.Add("zone", bucket);
            MemcachedConfiguration.Authentication.Parameters.Add("userName", username);
            MemcachedConfiguration.Authentication.Parameters.Add("password", password);
        }
    }
}
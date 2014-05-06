using System;
using System.Collections.Generic;
using System.Linq;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Enyim.Caching.Memcached.Results;
using Enyim.Caching.Memcached.Results.Extensions;
using System.Reflection;

namespace couchbase_net_rest
{
    public static class CouchbaseClientExtensions
    {
        public static JsonSerializerSettings JsonSerializerSettings;

        static CouchbaseClientExtensions()
        {
            JsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DocumentIdContractResolver()
            };
        }

        private const string Null = "null";

        #region No expiry

        public static IStoreOperationResult ExecuteStoreJson(this MemcachedClient client, StoreMode mode, string key, object value)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json);
        }

        public static bool StoreJson(this MemcachedClient client, StoreMode mode, string key, object value)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json).Success;
        }

        public static IStoreOperationResult ExecuteCasJson(this MemcachedClient client, StoreMode mode, string key, object value, ulong cas)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, cas);
        }

        public static bool CasJson(this MemcachedClient client, StoreMode mode, string key, object value, ulong cas)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, cas).Success;
        }

        #endregion

        #region DateTime expiry

        public static IStoreOperationResult ExecuteStoreJson(this MemcachedClient client, StoreMode mode, string key, object value, DateTime expiresAt)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json, expiresAt);
        }

        public static bool StoreJson(this MemcachedClient client, StoreMode mode, string key, object value, DateTime expiresAt)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json, expiresAt).Success;
        }

        public static IStoreOperationResult ExecuteCasJson(this MemcachedClient client, StoreMode mode, string key, object value, DateTime expiresAt, ulong cas)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, expiresAt, cas);
        }

        public static bool CasJson(this MemcachedClient client, StoreMode mode, string key, object value, ulong cas, DateTime expiresAt)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, expiresAt, cas).Success;
        }

        #endregion

        #region TimeSpan expiry

        public static IStoreOperationResult ExecuteStoreJson(this MemcachedClient client, StoreMode mode, string key, object value, TimeSpan validFor)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json, validFor);
        }

        public static bool StoreJson(this MemcachedClient client, StoreMode mode, string key, object value, TimeSpan validFor)
        {
            var json = SerializeObject(value);
            return client.ExecuteStore(mode, key, json, validFor).Success;
        }

        public static IStoreOperationResult ExecuteCasJson(this MemcachedClient client, StoreMode mode, string key, object value, TimeSpan validFor, ulong cas)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, validFor, cas);
        }

        public static bool CasJson(this MemcachedClient client, StoreMode mode, string key, object value, ulong cas, TimeSpan validFor)
        {
            var json = SerializeObject(value);
            return client.ExecuteCas(mode, key, json, validFor, cas).Success;
        }

        #endregion

        private static bool IsArrayOrCollection(Type type)
        {
            return type.GetInterface(typeof(IEnumerable<>).FullName) != null;
        }

        public static T GetJson<T>(this MemcachedClient client, string key) where T : class
        {
            var json = client.Get<string>(key);
            return json == null || json == Null ? null : DeserializeObject<T>(json);
        }

        public static IGetOperationResult<T> ExecuteGetJson<T>(this MemcachedClient client, string key) where T : class
        {
            var result = client.ExecuteGet<string>(key);
            var retVal = new GetOperationResult<T>();
            result.Combine(retVal);
            retVal.Cas = result.Cas;

            if (!result.Success)
            {
                return retVal;
            }
            retVal.Value = DeserializeObject<T>(result.Value);
            return retVal;
        }

        private static T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        private static string SerializeObject(object value)
        {
            var json = JsonConvert.SerializeObject(value,
                                    Formatting.None,
                                    JsonSerializerSettings);
            return json;
        }

        private class DocumentIdContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                return base.GetSerializableMembers(objectType).Where(o => o.Name != "Id").ToList();
            }
        }
    }
}

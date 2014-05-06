using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace couchbase_net_rest
{
    public interface IUrlBuilder
    {
        /// <summary>
        /// Get the lowest first level path of the url.  ex:  http://domain:port/first
        /// </summary>
        string RootServerPath { get; }

        /// <summary>
        /// Get the full path without any query string parameters.  ex: http://domain:port/first/second/third
        /// </summary>
        string LocalServerPath { get; }

        /// <summary>
        /// The final Uri object which contains combined parts in this Url.
        /// </summary>
        Uri Address { get; }

        /// <summary>
        /// Add a path command to the Url.  Note these are stacked in order.
        /// </summary>
        /// <param name="command">List of paths to stack onto the Url.</param>
        IUrlBuilder AddCommand(params string[] command);

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        IUrlBuilder AddParameter(string key, string value);

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        IUrlBuilder AddParameter(string key, object value);

        /// <summary>
        /// Gets a clean copy of the SolrUrl with the original base server path.
        /// </summary>
        /// <returns></returns>
        IUrlBuilder CloneBase();

        /// <summary>
        /// Clones an exact copy of this full url in its currently built state.
        /// </summary>
        /// <returns></returns>
        IUrlBuilder Clone();

        /// <summary>
        /// Merges another builders command and parameters to the the base uri of this builder
        /// </summary>
        /// <returns></returns>
        IUrlBuilder Merge(IQueryBuilder builder);
    }

    /// <summary>
    /// Helper class which providers a programatic way to build a Url one piece at a time.  
    /// </summary>
    public class UrlBuilder : IUrlBuilder
    {
        #region Private Members...
        private readonly Uri _sourceAddress;
        private List<string> _commands = new List<string>();
        private NameValueCollection _parameters;
        #endregion

        #region Public Properties...
        /// <summary>
        /// Get the lowest first level path of the url.  ex:  http://domain:port/first
        /// </summary>
        public string RootServerPath { get { return string.Format("{0}://{1}{2}", _sourceAddress.Scheme, _sourceAddress.Authority, _sourceAddress.AbsolutePath); } }

        /// <summary>
        /// Get the full path without any query string parameters.  ex: http://domain:port/first/second/third
        /// </summary>
        public string LocalServerPath
        {
            get
            {
                var address = Address;
                return string.Format("{0}://{1}{2}", address.Scheme, address.Authority, address.LocalPath);
            }
        }

        /// <summary>
        /// The final Uri object which contains combined parts in this Url.
        /// </summary>
        public Uri Address
        {
            get
            {
                return new Uri(BuildUrl());
            }
        }
        #endregion

        #region Constructors...
        public UrlBuilder()
        {

        }

        /// <summary>
        /// Construct a new UriBuilder with an initial Uri 
        /// </summary>
        /// <param name="connectionUri"></param>
        public UrlBuilder(Uri connectionUri)
        {
            _sourceAddress = connectionUri;
            _parameters = System.Web.HttpUtility.ParseQueryString(_sourceAddress.Query);
        }

        /// <summary>
        /// Construct a new UriBuilder with an initial url string
        /// </summary>
        /// <param name="connectionString"> </param>
        public UrlBuilder(string connectionString)
        {
            _sourceAddress = new Uri(connectionString);
            _parameters = System.Web.HttpUtility.ParseQueryString(_sourceAddress.Query);

            if (_sourceAddress == null)
                throw new Exception(string.Format("Conncetion string did not properly parce into a Uri. ConnectionString: {0}", connectionString));
        }
        #endregion

        #region Public Method...
        /// <summary>
        /// Add a path command to the Url.  Note these are stacked in order.
        /// </summary>
        /// <param name="command">List of paths to stack onto the Url.</param>
        public IUrlBuilder AddCommand(params string[] command)
        {
            foreach (var c in command)
            {
                _commands.Add(c);
            }

            return this;
        }

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        public IUrlBuilder AddParameter(string key, string value)
        {
            _parameters.Add(key, value);

            return this;
        }

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        public IUrlBuilder AddParameter(string key, object value)
        {
            _parameters.Add(key, value.ToString());

            return this;
        }

        /// <summary>
        /// Gets a clean copy of the SolrUrl with the original base server path.
        /// </summary>
        /// <returns></returns>
        public IUrlBuilder CloneBase()
        {
            return new UrlBuilder(RootServerPath);
        }

        /// <summary>
        /// Clones an exact copy of this full url in its currently built state.
        /// </summary>
        /// <returns></returns>
        public IUrlBuilder Clone()
        {
            return new UrlBuilder(Address);
        }

        /// <summary>
        /// Takes the base uri of this object and replaces the command and parameter data from IQueryBuilder
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public IUrlBuilder Merge(IQueryBuilder builder)
        {
            _commands = builder.Commands;
            _parameters = builder.Parameters;
            return this;
        }

        #endregion

        #region Private Method...
        private string BuildUrl()
        {
            var url = RootServerPath;
            var commands = string.Join("/", _commands);
            if (string.IsNullOrEmpty(commands) == false)
                url = string.Format("{0}/{1}", url, commands);

            return _parameters.Count > 0 ? string.Format("{0}?{1}", url, _parameters) : url;
        }
        #endregion
    }

    public interface IQueryBuilder
    {
        List<string> Commands { get; }
        NameValueCollection Parameters { get; }

        /// <summary>
        /// Add a path command to the Url.  Note these are stacked in order.
        /// </summary>
        /// <param name="command">List of paths to stack onto the Url.</param>
        IQueryBuilder AddCommand(params string[] command);

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        IQueryBuilder AddParameter(string key, string value);

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        IQueryBuilder AddParameter(string key, object value);
    }

    public class QueryBuilder : IQueryBuilder
    {
        private readonly List<string> _commands = new List<string>();
        private readonly NameValueCollection _parameters;

        public List<string> Commands { get { return _commands; } }
        public NameValueCollection Parameters { get { return _parameters; } }

        public QueryBuilder(string clientId)
        {
            _parameters = System.Web.HttpUtility.ParseQueryString("client_id=" + clientId);
        }

        /// <summary>
        /// Add a path command to the Url.  Note these are stacked in order.
        /// </summary>
        /// <param name="command">List of paths to stack onto the Url.</param>
        public IQueryBuilder AddCommand(params string[] command)
        {
            foreach (var c in command)
            {
                _commands.Add(c);
            }

            return this;
        }

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        public IQueryBuilder AddParameter(string key, string value)
        {
            _parameters.Add(key, value);

            return this;
        }

        /// <summary>
        /// Add a querystring parameter to the Url.  Note UrlBuilder will manage encoding.
        /// </summary>
        /// <param name="key">Querystring key</param>
        /// <param name="value">Querystring value</param>
        public IQueryBuilder AddParameter(string key, object value)
        {
            _parameters.Add(key, value.ToString());

            return this;
        }

        public override string ToString()
        {
            return BuildUrl();
        }


        private string BuildUrl()
        {
            string url = "";
            var commands = string.Join("/", _commands);
            if (string.IsNullOrEmpty(commands) == false)
                url = string.Format("{0}/{1}", url, commands);

            return _parameters.Count > 0 ? string.Format("{0}?{1}", url, _parameters) : url;
        }
    }
}

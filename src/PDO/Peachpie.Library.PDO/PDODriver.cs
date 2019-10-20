﻿using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Reflection;

namespace Peachpie.Library.PDO
{
    /// <summary>
    /// PDO driver base class
    /// </summary>
    [PhpHidden]
    public abstract class PDODriver
    {
        /// <summary>
        /// Gets the driver name (used in DSN)
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the client version.
        /// </summary>
        /// <value>
        /// The client version.
        /// </value>
        public virtual string ClientVersion
        {
            get
            {
                return this.DbFactory.GetType().Assembly.GetName().Version.ToString();
            }
        }

        /// <inheritDoc />
        public DbProviderFactory DbFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PDODriver"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="dbFactory">The database factory object.</param>
        /// <exception cref="System.ArgumentNullException">
        /// name
        /// or
        /// dbFactory
        /// </exception>
        public PDODriver(string name, DbProviderFactory dbFactory)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.DbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <summary>
        /// Builds the connection string.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        protected abstract string BuildConnectionString(ReadOnlySpan<char> dsn, string user, string password, PhpArray options);

        /// <summary>
        /// Opens a new database connection.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public virtual DbConnection OpenConnection(ReadOnlySpan<char> dsn, string user, string password, PhpArray options)
        {
            var connection = this.DbFactory.CreateConnection();
            connection.ConnectionString = this.BuildConnectionString(dsn, user, password, options);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Gets the methods added to the PDO instance when this driver is used.
        /// Returns <c>null</c> if the method is not defined.
        /// </summary>
        public virtual ExtensionMethodDelegate TryGetExtensionMethod(string name) => null;

        /// <summary>
        /// Gets the last insert identifier.
        /// </summary>
        /// <param name="pdo">Reference to corresponding <see cref="PDO"/> instance.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public abstract string GetLastInsertId(PDO pdo, string name);

        /// <summary>
        /// Tries to set a driver specific attribute value.
        /// </summary>
        /// <param name="attributes">The current attributes collection.</param>
        /// <param name="attribute">The attribute to set.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if value is valid, or false if value can't be set.</returns>
        public virtual bool TrySetAttribute(Dictionary<PDO.PDO_ATTR, PhpValue> attributes, PDO.PDO_ATTR attribute, PhpValue value)
        {
            return false;
        }

        /// <summary>
        /// Quotes a string for use in a query.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="param">The parameter.</param>
        /// <returns></returns>
        public virtual string Quote(string str, PDO.PARAM param)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes DB exception and returns corresponding error info.
        /// </summary>
        public virtual void HandleException(Exception ex, out string SQLSTATE, out string code, out string message)
        {
            SQLSTATE = string.Empty;
            code = null;
            message = ex.Message;
        }

        /// <summary>
        /// Gets the driver specific attribute value
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        public virtual PhpValue GetAttribute(PDO pdo, int attribute)
        {
            return PhpValue.Null;
        }

        /// <summary>
        /// Opens a DataReader.
        /// </summary>
        /// <param name="pdo">The pdo.</param>
        /// <param name="cmd">The command.</param>
        /// <param name="cursor">The cursor configuration.</param>
        /// <returns></returns>
        public virtual DbDataReader OpenReader(PDO pdo, DbCommand cmd, PDO.PDO_CURSOR cursor)
        {
            return cmd.ExecuteReader();
        }
    }
}

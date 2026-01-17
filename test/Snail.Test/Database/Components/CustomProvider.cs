using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Interfaces;
using Snail.Elastic;
using Snail.Mongo;
using Snail.MySql;
using Snail.PostgreSql;
using Snail.Test.Database.Interfaces;


namespace Snail.Test.Database.Components
{
    /// <summary>
    /// 
    /// </summary>
    [DbProvider<ICustomProvider>(DbType = DbType.MySql, Lifetime = LifetimeType.Singleton)]
    public sealed class MySqlCustomProvider : MySqlProvider, ICustomProvider
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        /// <param name="server"></param>
        public MySqlCustomProvider(IApplication app, IDbServerOptions server)
            : base(app, server)
        {
        }
    }
    /// <summary>
    /// 
    /// </summary>
    [DbProvider<ICustomProvider>(DbType = DbType.Postgres, Lifetime = LifetimeType.Singleton)]
    public sealed class PostgresCustomProvider : PostgresProvider, ICustomProvider
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        /// <param name="server"></param>
        public PostgresCustomProvider(IApplication app, IDbServerOptions server)
            : base(app, server)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [DbProvider<ICustomProvider>(DbType = DbType.MongoDB, Lifetime = LifetimeType.Singleton)]
    public sealed class MongoCustomProvider : MongoProvider, ICustomProvider
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        /// <param name="server"></param>
        public MongoCustomProvider(IApplication app, IDbServerOptions server)
            : base(app, server)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [DbProvider<ICustomProvider>(DbType = DbType.ElasticSearch, Lifetime = LifetimeType.Singleton)]
    public sealed class ElasticCustomProvider : ElasticProvider, ICustomProvider
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        /// <param name="server"></param>
        public ElasticCustomProvider(IApplication app, IDbServerOptions server)
            : base(app, server)
        {
        }
    }
}

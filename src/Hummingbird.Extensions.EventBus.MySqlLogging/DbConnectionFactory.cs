
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Hummingbird.Extensions.EventBus.MySqlLogging
{
    public  class DbConnectionFactory: IDbConnectionFactory
    {
        public readonly string ConnectionString;

        public DbConnectionFactory(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        public System.Data.Common.DbConnection GetDbConnection() {
            
            var connection = new MySql.Data.MySqlClient.MySqlConnection(this.ConnectionString);
            return connection;
        }
    }
}

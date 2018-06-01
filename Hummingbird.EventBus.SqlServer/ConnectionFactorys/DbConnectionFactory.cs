
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace Hummingbird.EventBus.SqlServer
{
    public  class DbConnectionFactory: IDbConnectionFactory
    {
        public readonly string ConnectionString;

        public DbConnectionFactory(string ConnectionString)
        {
            this.ConnectionString = ConnectionString;
        }

        public System.Data.Common.DbConnection GetDbConnection() {

            var connection = new SqlConnection(this.ConnectionString);
            return connection;
        }
    }
}

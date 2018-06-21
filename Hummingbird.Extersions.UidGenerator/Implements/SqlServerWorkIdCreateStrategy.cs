using System;
using System.Collections.Generic;
using System.Text;
using Dapper;
namespace Hummingbird.Extersions.UidGenerator.WorkIdCreateStrategy
{
    public class SqlServerWorkIdCreateStrategy : IWorkIdCreateStrategy
    {
        private readonly string _ConnectionString;
        private readonly string _WorkTag;

        public SqlServerWorkIdCreateStrategy(string ConnectionString,string WorkTag)
        {
            _ConnectionString = ConnectionString;
            _WorkTag = WorkTag;

        }
        
        /// <summary>
        /// 获取1~32之间的数字
        /// </summary>
        /// <returns></returns>
        public int NextId()
        {
            using (var connection = new System.Data.SqlClient.SqlConnection(_ConnectionString))
            {
                return connection.ExecuteScalar<int>(" if not exists(select object_id from sys.sequences where name='WorkId') begin CREATE SEQUENCE WorkId AS bigint START WITH 1 INCREMENT BY 1 MINVALUE 0 MAXVALUE 31 CYCLE CACHE 3 SELECT NEXT VALUE FOR WorkId end SELECT NEXT VALUE FOR WorkId");
            }
        }
    }
}

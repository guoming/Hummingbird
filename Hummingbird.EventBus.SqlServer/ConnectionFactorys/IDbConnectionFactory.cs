using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.EventBus.SqlServer
{
    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection GetDbConnection();
    }
}

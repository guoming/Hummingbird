using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.EventBus.SqlServerLogging
{
    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection GetDbConnection();
    }
}

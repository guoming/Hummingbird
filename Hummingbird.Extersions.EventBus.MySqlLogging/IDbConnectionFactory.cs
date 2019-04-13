using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extersions.EventBus.MySqlLogging
{
    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection GetDbConnection();
    }
}

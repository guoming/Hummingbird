using System;
using System.Collections.Generic;
using System.Text;

namespace Hummingbird.Extensions.EventBus.MySqlLogging
{
    public interface IDbConnectionFactory
    {
        System.Data.Common.DbConnection GetDbConnection();
    }
}

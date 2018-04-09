using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Hummingbird.Resilience.Transaction
{
    public class ResilientTransaction
    {
        private DbContext _context;
        private ResilientTransaction(DbContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public static ResilientTransaction New (DbContext context) =>
            new ResilientTransaction(context);        

        public async Task ExecuteAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    await action();
                    transaction.Commit();
                }
            });
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    var result=await action();
                    transaction.Commit();
                    return result;
                }
            });
        }
    }
}

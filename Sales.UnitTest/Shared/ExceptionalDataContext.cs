using Microsoft.EntityFrameworkCore;
using Sales.API.Data;

namespace Sales.UnitTest.Shared
{
    public class ExceptionalDataContext : DataContext
    {
        public ExceptionalDataContext(DbContextOptions<DataContext> options) : base(options)
        {
           
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated exception");
        }
    }
}

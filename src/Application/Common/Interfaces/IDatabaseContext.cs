using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Application.Common.Interfaces
{
    public interface IDatabaseContext
    {
        DatabaseFacade Database { get; }
        ChangeTracker ChangeTracker { get; }
        IModel Model { get; }

        IServiceProvider AsServiceProvider();

        DbSet<Order> Orders { get; set; }
        DbSet<Order> OrderStatuses { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}

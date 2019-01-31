using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using MySql;
using WomPlatform.Web.Api.DatabaseModels;

namespace WomPlatform.Web.Api {

    public class DataContext : DbContext {

        public string ConnectionString { get; }
        protected ILogger<DataContext> Logger;

        public DataContext(
            string connectionString,
            ILogger<DataContext> logger
        ) {
            ConnectionString = connectionString;
            Logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseMySQL(ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {

            // Fix boolean type conversion
            foreach(var entityType in modelBuilder.Model.GetEntityTypes()) {
                foreach(var property in entityType.GetProperties()) {
                    if(property.ClrType == typeof(bool)) {
                        property.SetValueConverter(new BoolToZeroOneConverter<short>());
                    }
                }
            }
        }

        public override void Dispose() {
            Logger.LogDebug(LoggingEvents.DatabaseConnection, "Disposing DataContext");
            base.Dispose();
        }

        public DbSet<Aim> Aims { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<GenerationRequest> GenerationRequests { get; set; }
        public DbSet<PaymentRequest> PaymentRequests { get; set; }
        public DbSet<POS> POS { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }

    }

}
using System;
using System.Linq;
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1
{
    
    class Program
    {
        private static MyContext _context=new MyContext();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            _context.Database.EnsureCreated();
            _context.Database.Migrate();
            _context.GetService<ILoggerFactory>().AddProvider(new MyLoggerProvider());


            var predicate = PredicateBuilder.New<MyData>(true);
            predicate = predicate.And(x => x.Id > 2);
            predicate = predicate.And(x => x.Description.Contains("Fred"));

            var res = _context.MyData
                .AsNoTracking()
                .AsExpandable()
                .Where(predicate)
                .ToList();

            Console.WriteLine("");
            Console.WriteLine($" records returned : {res.Count}");

            Console.ReadKey();
        }
    }

    public class MyLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new MyLogger();
        }

        public void Dispose()
        {
        }

        private class MyLogger : ILogger
        {
            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                //File.AppendAllText(@"C:\temp\log.txt", formatter(state, exception));
                Console.WriteLine(formatter(state, exception));
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }
    }

        public class MyData
    {
        public int Id { get; set; }
        public string Description { get; set; }

    }

    public class MyContext:DbContext
    {
        public DbSet<MyData> MyData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyData>()
                .HasKey(s => new { s.Id });

            modelBuilder.Entity<MyData>().Property(s => s.Id)
                .UseSqlServerIdentityColumn();

            modelBuilder.Entity<MyData>().Property(s => s.Description)
                .HasMaxLength(50)
                .HasDefaultValue("");

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=K3S-MLMK-DELL; Database=Play; Trusted_Connection=True");
        }
    }
}

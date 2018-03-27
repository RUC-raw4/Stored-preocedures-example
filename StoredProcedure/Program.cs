using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace StoredProcedure
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
    }

    public class SimpleCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class Context : DbContext
    {
        public const string ConnStr = "server=localhost;database=northwind;uid=bulskov;pwd=henrik";
        public DbSet<Category> Categories { get; set; }
        public DbSet<SimpleCategory> SimpleCategories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMySql(ConnStr);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SimpleCategory>().ToTable("categories");
            modelBuilder.Entity<SimpleCategory>().HasKey(x => x.CategoryId);
        }
    }

    /* The procedure to be used in this example
     
        CREATE DEFINER=`root`@`localhost` PROCEDURE `getcat`(s varchar(100), x int, y int)
        BEGIN
	        select * from categories
            where categoryname like concat(s, '%')
            or categoryid = x
            or categoryid = y;
        END
     */


    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Entity Framework:");
            UseEntityFramework();

            Console.WriteLine("Entity Framework with different object:");
            UseEntityFrameworkDiffObj();

            Console.WriteLine("ADO:");
            UseStdConnection();
        }

        private static void UseEntityFramework()
        {
            using (var db = new Context())
            {

                var result = db.Categories.FromSql("call getcat({0},{1},{2})", "s", 1, 2);
                foreach (var category in result)
                {
                    Console.WriteLine("{0}, {1}, {2}", category.CategoryId, category.CategoryName, category.Description);
                }
            }
        }

        // If you need to return another type than objects from the domaine model
        // you can do it like this - see the Context for configuring
        // Note: you need to specify the primary key in the config
        private static void UseEntityFrameworkDiffObj()
        {
            using (var db = new Context())
            {
                // you can also use the string interpolation syntax
                var str = "s";
                var id1 = 1;
                var id2 = 2;
                var result = db.SimpleCategories.FromSql($"call getcat({str},{id1},{id2})");
                foreach (var category in result)
                {
                    Console.WriteLine($"{category.CategoryId}, {category.CategoryName}");
                }
            }
        }

        private static void UseStdConnection()
        {
            using (var db = new Context())
            {
                var conn = (MySqlConnection)db.Database.GetDbConnection();
                conn.Open();
                var cmd = new MySqlCommand();
                cmd.Connection = conn;

                cmd.Parameters.Add("@1", DbType.String);
                cmd.Parameters.Add("@2", DbType.Int32);
                cmd.Parameters.Add("@3", DbType.Int32);

                cmd.Parameters["@1"].Value = "s";
                cmd.Parameters["@2"].Value = 1;
                cmd.Parameters["@3"].Value = 2;


                cmd.CommandText = "call getcat(@1,@2,@3)";

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine("{0}, {1}, {2}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
                }
            }
        }
    }
}

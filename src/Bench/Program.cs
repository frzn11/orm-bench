
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Dapper; 
using Microsoft.Data.SqlClient;
using System.Linq;
using Bench.Models;
using Bench.Util; 
using Microsoft.EntityFrameworkCore; 
using NHibernate; 


using System.Collections.Generic;

int s_size=300;
int m_size=3000;
int l_size=20000;

static int[] GenerateIntegers(int a, int b, int n)
{
    if (n > (b - a + 1))
        Console.Error.WriteLine("n cannot be larger than the size of the range [a, b].");

    Random rng = new Random();
    return Enumerable.Range(a, b - a + 1)  // all numbers from a to b
                     .OrderBy(x => rng.Next()) // shuffle
                     .Take(n)                  // take n distinct
                     .ToArray();
}


static string[] GenerateStrings(int n)
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    Random rng = new Random();
    string[] results = new string[n];

    for (int i = 0; i < n; i++)
    {
        char[] strChars = new char[7];
        for (int j = 0; j < 7; j++)
        {
            strChars[j] = chars[rng.Next(chars.Length)];
        }
        results[i] = new string(strChars);
    }

    return results;
}



static int ReadCpuTempC()
{
    string txt = File.ReadAllText("/sys/class/thermal/thermal_zone8/temp").Trim();
    return int.Parse(txt) / 1000;  
}


static void WaitForCoolDown(int thresholdC, int waitSeconds)
{
    int temp = ReadCpuTempC();
    while (temp > thresholdC)
    {
        Console.Error.WriteLine($"CPU too hot: {temp}°C, waiting {waitSeconds}s...");
        Thread.Sleep(waitSeconds * 1000);
        temp = ReadCpuTempC();
    }
}


static (string db, string small, string medium, string large) ReadTableMap(string path)
{
    var doc = JsonDocument.Parse(File.ReadAllText(path));
    var root = doc.RootElement;
    var db = root.GetProperty("database").GetString()!;
    var sizes = root.GetProperty("sizes");
    return (db,
        sizes.GetProperty("small").GetString()!,
        sizes.GetProperty("medium").GetString()!,
        sizes.GetProperty("large").GetString()!
    );
}

static string TableFor(string size, (string db, string s, string m, string l) map)
    => size.ToLower() switch {
        "small"  => map.s,
        "medium" => map.m,
        "large"  => map.l,
        _ => throw new ArgumentException("size must be small|medium|large")
    };

string orm  = "dapper";
string op   = "getall";
string size = "small";
int reps    = 1;
string mapPath = "config/tables.json";
string saPwd = "CAPstone1";


for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--orm": orm = args[++i]; break;
        case "--op": op = args[++i]; break;
        case "--size": size = args[++i]; break;
        case "--reps": reps = int.Parse(args[++i]); break;
        case "--map": mapPath = args[++i]; break;
        case "--sapwd": saPwd = args[++i]; break;
    }
}

int n=0;
switch (size){
    case "small": n=s_size ; break;
    case "medium": n=m_size ; break;
    case "large": n=l_size ; break;

}

var map = ReadTableMap(mapPath);
string connStr = $"Server=localhost,1433;Database={map.db};User Id=sa;Password={saPwd};TrustServerCertificate=true;";
string table = TableFor(size, (map.db, map.small, map.medium, map.large));

//WaitForCoolDown(thresholdC: 50, waitSeconds: 10);
var rapl = new RaplMeter();       
double startEnergy=0, endEnergy=0;
long time=0;
int rowsAffected = 0;





switch (orm)
{
    case "dapper":
        switch (op)
        {
            case "getall":{
                
                        using (var conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = $"SELECT * FROM {table}";
                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var rows = conn.Query<SmallTable>(sql);
                            }
                            sw.Stop();
                            time=sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                            conn.Close();
                        }
                
                break;}

            case "getbyid":{
                        int[] ints = GenerateIntegers(1,n,n);
                        using (var conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = $"SELECT * FROM {table} WHERE Id = @Id";

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var row = conn.QuerySingleOrDefault<SmallTable>(sql, new { Id = ints[i%n] });

                            }

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();

                            conn.Close();
                        }
            break;}

            case "create":{
                
                        int[] ints1 = GenerateIntegers(100,10000,100);
                        int[] ints2 = GenerateIntegers(100,10000,100);
                        int[] ints3 = GenerateIntegers(100,10000,100);
                        int[] ints4 = GenerateIntegers(100,10000,100);
                        int[] ints5 = GenerateIntegers(100,10000,100);
                        string[] strs1 = GenerateStrings(100);
                        string[] strs2 = GenerateStrings(100);
                        string[] strs3 = GenerateStrings(100);
                        string[] strs4 = GenerateStrings(100);

                        var entities = new List<SmallTable>(reps);
                        int x = n+1;
                        for (int i = x; i < x + reps; i++)
                        {
                            int j=(i - x) % 100;
                            entities.Add(new SmallTable
                            {
                                id = i,
                                int_col1 = ints1[j],
                                int_col2 = ints2[j],
                                int_col3 = ints3[j],
                                int_col4 = ints4[j],
                                int_col5 = ints5[j],
                                str_col1 = strs1[j],
                                str_col2 = strs2[j],
                                str_col3 = strs3[j],
                                str_col4 = strs4[j]
                            });
                        }


                        using (var conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = $@"
                                INSERT INTO {table} 
                                (id, int_col1, int_col2, int_col3, int_col4, int_col5, str_col1, str_col2, str_col3, str_col4)
                                VALUES (@id, @int_col1, @int_col2, @int_col3, @int_col4, @int_col5, @str_col1, @str_col2, @str_col3, @str_col4)";

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);
                                
                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();


                            foreach (var e in entities)
                                rowsAffected += conn.Execute(sql, e);


                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();

                            conn.Close();
                        }
                    break;}

            case "delete":{
                    int[] ints = GenerateIntegers(1,reps+n,reps);

                        using (var conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = $"DELETE FROM {table} WHERE id = @id";

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);



                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                rowsAffected += conn.Execute(sql, new { id = ints[i] });
                            }

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();

                            conn.Close();
                        }
                    break;}
 

            case "update":{
                
                    int[] ints = GenerateIntegers(1,n,n);
                    string[] strs = GenerateStrings(100);

                        using (var conn = new SqlConnection(connStr))
                        {
                            conn.Open();
                            string sql = $"UPDATE {table} SET str_col1 = @str_col1 WHERE id = @id";

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                rowsAffected += conn.Execute(sql, new { id = ints[i%n], str_col1 = strs[i%100] });
                            }

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();

                            conn.Close();
                        }

                    break;}


        }
        break;

    
    case "efcore":
        switch (op)
        {
            case "getall":{
                        var options = new DbContextOptionsBuilder<BenchDbContext>()
                                        .UseSqlServer(connStr)
                                        .Options;



                        using (var ctx = new Bench.Models.BenchDbContext(options,table))
                        {   


                        WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                        startEnergy = rapl.ReadJoules();
                        var sw = Stopwatch.StartNew();

                        for (int i = 0; i < reps; i++)
                        {
                            var rows = ctx.SmallTable.AsNoTracking().ToList();
                            rowsAffected += rows.Count;
                        }

                        sw.Stop();
                        time = sw.ElapsedMilliseconds;
                        endEnergy = rapl.ReadJoules();
                    }
                    break;}

            case "getbyid":{

                        int[] ints = GenerateIntegers(1,n,n);

                        var options = new DbContextOptionsBuilder<BenchDbContext>()
                                        .UseSqlServer(connStr)
                                        .Options;



                        using (var context = new Bench.Models.BenchDbContext(options,table))
                        {   



                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {

                                var result = context.SmallTable.AsNoTracking().Single(x => x.id == ints[i%n]);
                            }

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }
                    break;}


            case "create":{

                        int[] ints1 = GenerateIntegers(100,10000,100);
                        int[] ints2 = GenerateIntegers(100,10000,100);
                        int[] ints3 = GenerateIntegers(100,10000,100);
                        int[] ints4 = GenerateIntegers(100,10000,100);
                        int[] ints5 = GenerateIntegers(100,10000,100);
                        string[] strs1 = GenerateStrings(100);
                        string[] strs2 = GenerateStrings(100);
                        string[] strs3 = GenerateStrings(100);
                        string[] strs4 = GenerateStrings(100);

                        var entities = new List<SmallTable>(reps);

                        for (int i = n+1; i < n+1 + reps; i++)
                        {
                            int j=(i - n-1) % 100;
                            entities.Add(new SmallTable
                            {
                                id = i,
                                int_col1 = ints1[j],
                                int_col2 = ints2[j],
                                int_col3 = ints3[j],
                                int_col4 = ints4[j],
                                int_col5 = ints5[j],
                                str_col1 = strs1[j],
                                str_col2 = strs2[j],
                                str_col3 = strs3[j],
                                str_col4 = strs4[j]
                            });
                        }

                        var options = new DbContextOptionsBuilder<BenchDbContext>()
                                        .UseSqlServer(connStr)
                                        .Options;



                        using (var context = new Bench.Models.BenchDbContext(options,table))
                         
                        {
                            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            foreach (var e in entities)
                            {


                                context.Add(e);
                                rowsAffected += context.SaveChanges();
                            }

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }

                    break;}


            case "delete":{
                        int[] ints = GenerateIntegers(1,n+reps,reps);

                        var options = new DbContextOptionsBuilder<BenchDbContext>()
                                        .UseSqlServer(connStr)
                                        .Options;



                        using (var context = new Bench.Models.BenchDbContext(options,table))
                        {   


                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            using var tx = context.Database.BeginTransaction();

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var row = context.SmallTable.AsNoTracking().FirstOrDefault(r => r.id == ints[i]);
                                if (row!=null){
                                    context.SmallTable.Remove(row);
                                    rowsAffected += context.SaveChanges();
                                }
                                else{
                                    Console.Error.WriteLine("ef del error");
                                }

                            }
                            tx.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }

                    break;}

                case "update":{

                    int[] ids = GenerateIntegers(1, n, n);
                    string[] strs = GenerateStrings(100);
                        var options = new DbContextOptionsBuilder<BenchDbContext>()
                                        .UseSqlServer(connStr)
                                        .Options;



                        using (var context = new Bench.Models.BenchDbContext(options,table))
                        {   


                        WaitForCoolDown(thresholdC: 50, waitSeconds: 10);
                        using var tx = context.Database.BeginTransaction();

                        startEnergy = rapl.ReadJoules();
                        var sw = Stopwatch.StartNew();

                        for (int i = 0; i < reps; i++)
                        {
        

                            var row = context.SmallTable.AsNoTracking().FirstOrDefault(r => r.id == ids[i%(n)]);

                            if (row != null)
                            {
                                row.str_col1 = strs[i%100];

                                context.Update(row); 
                                            // context.Attach(row);
                                            // context.Entry(row).Property(e => e.str_col1).IsModified = true;


                                rowsAffected += context.SaveChanges();
                                context.ChangeTracker.Clear();

                            }
                        }

                        tx.Commit();

                        sw.Stop();
                        time = sw.ElapsedMilliseconds;
                        endEnergy = rapl.ReadJoules();
                    }

                break;}

        }
    break;

    case "nhib":
        switch (op)
        {
            case "getall":{
                        var sessionFactory = NHibernateHelper.BuildSessionFactory(connStr, table);
                        using (var session = sessionFactory.OpenSession())
                        using (var transaction = session.BeginTransaction())
                        {

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var results = session.Query<SmallTable>().ToList();
                                rowsAffected += results.Count;
                            }
                            transaction.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();

                        }


 
 
                    break;}


            case "getbyid":{

                        int[] ints = GenerateIntegers(1,n,n);

                        var sessionFactory = NHibernateHelper.BuildSessionFactory(connStr, table);
                        using (var session = sessionFactory.OpenSession())
                        using (var transaction = session.BeginTransaction())
                        {

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var query = session.Query<SmallTable>().FirstOrDefault(r => r.id == ints[(i+1)%n]);

                            }

                            transaction.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }



                    break;}


            case "create":{
                        int[] ints1 = GenerateIntegers(100,10000,100);
                        int[] ints2 = GenerateIntegers(100,10000,100);
                        int[] ints3 = GenerateIntegers(100,10000,100);
                        int[] ints4 = GenerateIntegers(100,10000,100);
                        int[] ints5 = GenerateIntegers(100,10000,100);
                        string[] strs1 = GenerateStrings(100);
                        string[] strs2 = GenerateStrings(100);
                        string[] strs3 = GenerateStrings(100);
                        string[] strs4 = GenerateStrings(100);

                        var entities = new List<SmallTable>(reps);

                        for (int i = n+1; i < n+1 + reps; i++)
                        {
                            int j=(i - n-1) % 100;
                            entities.Add(new SmallTable
                            {
                                id = i,
                                int_col1 = ints1[j],
                                int_col2 = ints2[j],
                                int_col3 = ints3[j],
                                int_col4 = ints4[j],
                                int_col5 = ints5[j],
                                str_col1 = strs1[j],
                                str_col2 = strs2[j],
                                str_col3 = strs3[j],
                                str_col4 = strs4[j]
                            });
                        }

                        var sessionFactory = NHibernateHelper.BuildSessionFactory(connStr, table);
                        using (var session = sessionFactory.OpenSession())
                        using (var transaction = session.BeginTransaction())
                        {

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);


                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            foreach (var e in entities){
                                session.Save(e);
                                rowsAffected++;
                            }
                                


                            transaction.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }



                    break;}



            case "delete":{



                        int[] ints = GenerateIntegers(1,reps+n,reps);


                        var sessionFactory = NHibernateHelper.BuildSessionFactory(connStr, table);
                        using (var session = sessionFactory.OpenSession())
                        using (var transaction = session.BeginTransaction())
                        {

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var row = session.Get<SmallTable>(ints[i]);   
                                session.Delete(row);
                            }

                            transaction.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }



                    break;}


            case "update":{

                        int[] ints = GenerateIntegers(1,n,n);
                        string[] strs = GenerateStrings(100);

                        var sessionFactory = NHibernateHelper.BuildSessionFactory(connStr, table);
                        using (var session = sessionFactory.OpenSession())
                        using (var transaction = session.BeginTransaction())
                        {

                            WaitForCoolDown(thresholdC: 50, waitSeconds: 10);

                            startEnergy = rapl.ReadJoules();
                            var sw = Stopwatch.StartNew();

                            for (int i = 0; i < reps; i++)
                            {
                                var row = session.Get<SmallTable>(ints[i%(n)]);

                                row.str_col1 = strs[i%100]; 
                                rowsAffected++;                

                            }

                            transaction.Commit();

                            sw.Stop();
                            time = sw.ElapsedMilliseconds;
                            endEnergy = rapl.ReadJoules();
                        }

        

                    break;}
        }

    break;

}


double deltaJ = rapl.Delta(startEnergy, endEnergy);

Console.WriteLine(JsonSerializer.Serialize(new {
    orm,
    op,
    size,
    table,
    reps,
    rows_affected = rowsAffected,
    duration_ms = time,
    energy_j = deltaJ,
    timestamp = DateTime.UtcNow.ToString("o")
}));

// RAPL helper
// public class RaplMeter
// {
//     private readonly string _path = "/sys/class/powercap/intel-rapl:0/energy_uj";
//     public double ReadJoules()
//     {
//         string txt = File.ReadAllText(_path).Trim();
//         return long.Parse(txt) / 1e6; // microjoules → joules
//     }
// }

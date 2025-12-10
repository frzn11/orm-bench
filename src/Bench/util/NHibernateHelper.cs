
// using NHibernate;
// using NHibernate.Cfg;
// using NHibernate.Mapping.ByCode;

// public static class NHibernateHelper
// {
//     public static ISessionFactory BuildSessionFactory(string connectionString)
//     {
//         var cfg = new Configuration();
//         cfg.DataBaseIntegration(db =>
//         {
//             db.ConnectionString = connectionString;
//             db.Dialect<NHibernate.Dialect.MsSql2012Dialect>();
//             db.Driver<NHibernate.Driver.MicrosoftDataSqlClientDriver>();
//         });

//         var mapper = new ModelMapper();
//         mapper.AddMapping<SmallTableMap>();
//         cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

//         cfg.CurrentSessionContext<NHibernate.Context.ThreadStaticSessionContext>();
//         cfg.SetProperty(NHibernate.Cfg.Environment.UseProxyValidator, "false");

//         return cfg.BuildSessionFactory();
//     }
// }


using NHibernate;
using NHibernate.Cfg;
using NHibernate.Mapping.ByCode;
using NHibernate.Dialect;
using NHibernate.Driver;
using Bench.Models;

public static class NHibernateHelper
{
    // Overload includes dynamic table name
    public static ISessionFactory BuildSessionFactory(string connectionString, string tableName)
    {
        var cfg = new Configuration();
        cfg.DataBaseIntegration(db =>
        {
            db.ConnectionString = connectionString;
            db.Dialect<MsSql2012Dialect>();
            db.Driver<MicrosoftDataSqlClientDriver>();
        });

        var mapper = new ModelMapper();
        mapper.Class<SmallTable>(m =>
        {
            m.Table(tableName);  // ✅ dynamic table name here
            m.Id(x => x.id, idm => idm.Generator(Generators.Assigned));
            m.Property(x => x.int_col1);
            m.Property(x => x.int_col2);
            m.Property(x => x.int_col3);
            m.Property(x => x.int_col4);
            m.Property(x => x.int_col5);
            m.Property(x => x.str_col1);
            m.Property(x => x.str_col2);
            m.Property(x => x.str_col3);
            m.Property(x => x.str_col4);
        });

        cfg.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());


        cfg.CurrentSessionContext<NHibernate.Context.ThreadStaticSessionContext>();
        cfg.SetProperty(NHibernate.Cfg.Environment.UseProxyValidator, "false");
        cfg.SetProperty(NHibernate.Cfg.Environment.BatchSize, "0");

        cfg.SetProperty(NHibernate.Cfg.Environment.GenerateStatistics, "true");


        return cfg.BuildSessionFactory();
    }
}

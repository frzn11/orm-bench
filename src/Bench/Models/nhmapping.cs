
// using NHibernate.Mapping.ByCode;
// using NHibernate.Mapping.ByCode.Conformist;
// using Bench.Models;

// public class SmallTableMap : ClassMapping<SmallTable>
// {
//     public SmallTableMap()
//     {
//         Table("SmallTable");
//         Id(x => x.id, m => m.Generator(Generators.Assigned));
//         Property(x => x.int_col1);
//         Property(x => x.int_col2);
//         Property(x => x.int_col3);
//         Property(x => x.int_col4);
//         Property(x => x.int_col5);
//         Property(x => x.str_col1);
//         Property(x => x.str_col2);
//         Property(x => x.str_col3);
//         Property(x => x.str_col4);
//     }
// }
// public class SmallTableMap : ClassMapping<SmallTable>
// {
//     public SmallTableMap()
//     {
//         Table(MappingOptions.tableName); // <— was "SmallTable"
//         Id(x => x.id, m => m.Generator(Generators.Assigned));
//         Property(x => x.int_col1);
//         Property(x => x.int_col2);
//         Property(x => x.int_col3);
//         Property(x => x.int_col4);
//         Property(x => x.int_col5);
//         Property(x => x.str_col1);
//         Property(x => x.str_col2);
//         Property(x => x.str_col3);
//         Property(x => x.str_col4);
//     }
// }

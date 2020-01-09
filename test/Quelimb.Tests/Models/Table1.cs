#nullable disable warnings
using System.ComponentModel.DataAnnotations.Schema;

namespace Quelimb.Tests.Models
{
    public class Table1
    {
        public int Id { get; set; }

        [Column("FooColumn")]
        public string ColumnName { get; set; }

        public int IntField;

        private int PrivateField;

        [NotMapped]
        public int Excluded { get; set; }
    }
}

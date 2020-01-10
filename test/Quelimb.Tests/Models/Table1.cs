#nullable disable warnings
#pragma warning disable 169 // private field is never used

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

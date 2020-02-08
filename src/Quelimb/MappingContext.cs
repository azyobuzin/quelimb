using System.Collections.Generic;

namespace Quelimb
{
    public abstract class MappingContext
    {
        public abstract int ColumnCount { get; }

        public virtual IReadOnlyList<string>? ColumnNames => null;
    }
}

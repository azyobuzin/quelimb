# Quelimb
A micro-ORM for complex queries.

## Features
### Typo-less
Use string interpolation, then the table names and column names are expanded.

```csharp
class Table1
{
    public int Column1 { get; set; }
    public int Column2 { get; set; }
}

List<Table1> results = QueryBuilder.Default
    .Query<Table1>(t1 => // `t1` represents Table1
        $"SELECT {t1.Column1}, {t1.Column2} FROM {t1}"
    )
    .Map<Table1>() // The result records will be mapped to Table1 object
    .ExecuteQuery(connection).ToList();

// ==> SELECT "Table1"."Column1", "Table1"."Column2" FROM "Table1"
```

### Process JOINed Records
You can write a query that selects columns of multiple tables. The records can be mapped as you want.

```csharp
class Table2
{
    public int ColumnA { get; set; }
    public string ColumnB { get; set; }
}

var results = QueryBuilder.Default
    .Query<Table1, Table2>((t1, t2) =>
        $@"SELECT {t1:*}, {t2.ColumnB}
           FROM {t1}
           INNER JOIN {t2} ON {t1.Column1} = {t2.ColumnA}"
    )
    .Map((Table1 t1, string ColumnB) => // All columns of Table1 and a string
        new { t1.Column1, ColumnB }) // Map to an anonymous object
    .ExecuteQuery(connection).ToList();

// ==> SELECT "Table1"."Column1", "Table1"."Column2", "Table2"."ColumnB"
//     FROM "Table1"
//     INNER JOIN "Table2" ON "Table1"."Column1" = "Table2"."ColumnA"
```

## Format Reference
### Tables and Columns
- `{table}` => `"Table"`
- `{table.Column}` => `"Table"."Column"`
- `{table.Column:C}` => `"Column"`

### All Columns of a Table
- `{table:*}` => `"Table"."Column1", "Table"."Column2"`

### Table Alias
- `SELECT {table.Column} FROM {table: AS T}` => `SELECT T."Column" FROM "Table" AS T`

### Other Values
- `{value}` => `@QuelimbParam0` (passed to `DbParameterCollection`)
- `{value:align,format}` => `@QuelimbParam1` (passed to `DbParameterCollection` as a formatted string value)

## Roadmap
- Performance
    - Expression trees are compiled every time. We need to cache compiled delegate.
    - Avoid boxing. This can be achieved by transforming expression trees to make not go through `FormattableString` instances.
- More flexible object mapping
    - The current `TableMapper` and `ValueConverter` are not deliberate.
- `INSERT` SQL generation.

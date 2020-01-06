namespace Quelimb.SqlGenerators
{
    public interface ISqlGenerator
    {
        string EscapeIdentifier(string identifier);
    }
}

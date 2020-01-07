namespace Quelimb
{
    public class RawQuery
    {
        public string Content { get; }

        public RawQuery(string content)
        {
            this.Content = content ?? "";
        }

        public override string ToString()
        {
            return this.Content;
        }

        public override bool Equals(object obj)
        {
            return obj is RawQuery r && Equals(this.Content, r.Content);
        }

        public override int GetHashCode()
        {
            return this.Content.GetHashCode();
        }
    }
}

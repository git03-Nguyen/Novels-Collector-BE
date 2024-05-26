namespace NovelsCollector.Core.Models.Novels
{
    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Novel[] Novels { get; set; }
    }
}

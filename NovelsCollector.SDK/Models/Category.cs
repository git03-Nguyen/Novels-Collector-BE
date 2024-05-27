namespace NovelsCollector.SDK.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Novel[] Novels { get; set; }
    }
}

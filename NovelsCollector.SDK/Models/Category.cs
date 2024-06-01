﻿namespace NovelsCollector.SDK.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string? Slug { get; set; }
        public string? Source { get; set; }
        public string? Name { get; set; }
        public Novel[]? Novels { get; set; }
    }
}

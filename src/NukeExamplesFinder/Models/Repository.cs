using System;
using System.Collections.Generic;
using System.Text;

namespace NukeExamplesFinder.Models
{
    public class Repository
    {
        public long Id { get; set; }
        // status values
        public DateTime FirstIndexed { get; set; }
        public DateTime LastIndexUpdated { get; set; }
        public DateTime LastDetailUpdated { get; set; }
        // primary values
        public string Name { get; set; }
        public string Owner { get; set; }
        public string HtmlUrl { get; set; }
        // details values
        public string Description { get; set; }
        public bool Archived { get; set; }
        public int Stars { get; set; }
        public int Watchers { get; set; }
        public string BuildFileHtmlUrl { get; set; }
        public string BuildFileName { get; set; }
    }
}

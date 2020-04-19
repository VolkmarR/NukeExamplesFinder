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
        public DateTime LastBuildFileUpdated { get; set; }

        // primary values
        public string Name { get; set; }
        public string Owner { get; set; }
        public string HtmlUrl { get; set; }

        // details values
        public string Description { get; set; }
        public bool Archived { get; set; }
        public int Stars { get; set; }
        public int Watchers { get; set; }

        // build file value
        public string BuildFilePath { get; set; }
        public string BuildFileUrl { get; set; }
        public int BuildFileSize { get; set; }
        public string BuildFileContent { get; set; }
    }
}

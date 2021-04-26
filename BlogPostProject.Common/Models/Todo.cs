using System;
using System.Collections.Generic;
using System.Text;

namespace BlogPostProject.Common.Models
{
    public class Todo
    {
        public string id { get; set; }
        public string Name { get; set; }
        public bool Complete { get; set; }
        public DateTime DateAdded { get; set; }
    }
}

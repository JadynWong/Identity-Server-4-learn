using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api1Resource.Models
{
    public class ToDoEdit
    {
        public string Title { get; internal set; }
        public bool Completed { get; internal set; }
    }
}

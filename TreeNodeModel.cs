using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyNote
{
    public class TreeNodeModel
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public List<TreeNodeModel> Children { get; set; } = new List<TreeNodeModel>();
    }
}

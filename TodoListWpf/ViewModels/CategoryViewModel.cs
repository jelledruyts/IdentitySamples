using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoListWpf.ViewModels
{
    public class CategoryViewModel
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public bool IsPrivate { get; private set; }
        public string DisplayName { get; private set; }

        public CategoryViewModel(string id, string name, bool isPrivate)
        {
            this.Id = id;
            this.Name = name;
            this.IsPrivate = isPrivate;
            this.DisplayName = this.Name;
            if (this.IsPrivate)
            {
                this.DisplayName += " [Private]";
            }
        }
    }
}
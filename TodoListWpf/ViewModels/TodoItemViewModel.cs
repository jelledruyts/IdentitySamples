using TodoListWpf.Models;

namespace TodoListWpf.ViewModels
{
    public class TodoItemViewModel
    {
        public string Title { get; private set; }
        public string CategoryId { get; private set; }
        public string CategoryName { get; private set; }
        public bool CategoryIsPrivate { get; private set; }

        public TodoItemViewModel(TodoItem item, Category category)
        {
            this.Title = item.Title;
            this.CategoryId = item.CategoryId;
            if (category != null)
            {
                this.CategoryName = category.Name;
                this.CategoryIsPrivate = category.IsPrivate;
            }
        }
    }
}
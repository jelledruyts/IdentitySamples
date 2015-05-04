using System.Collections.Generic;

namespace TodoListWebApp.Models
{
    public class TodoListIndexViewModel
    {
        public IList<TodoItem> TodoItems { get; private set; }
        public IList<Category> Categories { get; private set; }

        public TodoListIndexViewModel(IList<TodoItem> todoItems, IList<Category> categories)
        {
            this.TodoItems = todoItems;
            this.Categories = categories;
        }
    }
}
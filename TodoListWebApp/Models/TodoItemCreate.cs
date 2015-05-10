
namespace TodoListWebApp.Models
{
    public class TodoItemCreate
    {
        public string Title { get; set; }
        public string CategoryId { get; set; }
        public string NewCategoryName { get; set; }
        public bool NewCategoryIsPrivate { get; set; }
    }
}
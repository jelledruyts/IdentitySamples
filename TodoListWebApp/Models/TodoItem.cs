using System.ComponentModel.DataAnnotations;

namespace TodoListWebApp.Models
{
    public class TodoItem
    {
        public string Title { get; set; }
        public string CategoryId { get; set; }
    }
}
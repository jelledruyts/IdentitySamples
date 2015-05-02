using System.ComponentModel.DataAnnotations;

namespace TodoListWebApp.Models
{
    public class TodoItem
    {
        [Required]
        public string Title { get; set; }
    }
}
using System;

namespace TodoListWebApi.Models
{
    public class TodoItem
    {
        public string Title { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset CreatedTime { get; set; }
    }
}
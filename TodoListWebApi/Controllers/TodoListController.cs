using Common;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using TodoListWebApi.Models;

namespace TodoListWebApi.Controllers
{
    public class TodoListController : ApiController
    {
        private static ConcurrentBag<TodoItem> database = new ConcurrentBag<TodoItem>();

        public IEnumerable<TodoItem> Get()
        {
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            return database.Where(t => t.UserId == userId);
        }

        public IHttpActionResult Post(TodoItem value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Title))
            {
                return BadRequest("Title is required");
            }
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            database.Add(new TodoItem { Title = value.Title, UserId = userId });
            return Ok();
        }
    }
}
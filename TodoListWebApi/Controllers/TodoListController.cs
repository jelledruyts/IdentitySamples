using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using TodoListWebApi.Models;

namespace TodoListWebApi.Controllers
{
    [Authorize]
    public class TodoListController : ApiController
    {
        #region In-Memory Database

        private static ConcurrentBag<TodoItemData> database = new ConcurrentBag<TodoItemData>();

        private class TodoItemData
        {
            public string Id { get; set; }
            public DateTimeOffset CreatedTime { get; set; }
            public string Title { get; set; }
            public string CategoryId { get; set; }
            public string UserId { get; set; }

            public TodoItemData(string title, string categoryId, string userId)
            {
                this.Id = Guid.NewGuid().ToString();
                this.CreatedTime = DateTimeOffset.UtcNow;
                this.Title = title;
                this.CategoryId = categoryId;
                this.UserId = userId;
            }
        }

        #endregion

        public IEnumerable<TodoItem> Get()
        {
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            return database.Where(t => t.UserId == userId).OrderBy(t => t.CreatedTime).Select(t => new TodoItem { Id = t.Id, Title = t.Title, CategoryId = t.CategoryId });
        }

        public IHttpActionResult Post(TodoItem value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Title))
            {
                return BadRequest("Title is required");
            }
            if (value == null || string.IsNullOrWhiteSpace(value.CategoryId))
            {
                return BadRequest("CategoryId is required");
            }
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            var data = new TodoItemData(value.Title, value.CategoryId, userId);
            database.Add(data);
            value.Id = data.Id;
            return Ok(value);
        }
    }
}
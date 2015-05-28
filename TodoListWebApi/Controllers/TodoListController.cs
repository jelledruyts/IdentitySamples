using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using TodoListWebApi.Models;

namespace TodoListWebApi.Controllers
{
    // [NOTE] To ensure that the caller is authorized, just place the [Authorize] attribute.
    // Optionally, include roles e.g. [Authorize(Roles = "Contributor")] which are
    // checked against the role claims in the token.
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

        /// <summary>
        /// Gets the todo items of the current user.
        /// </summary>
        public IEnumerable<TodoItem> Get()
        {
            // [NOTE] The ClaimsPrincipal.Current is automatically populated
            // by the Bearer Token middleware with the claims coming from the
            // authorization token.
            // When using application roles, these can be checked with the
            // standard mechanisms, e.g. ClaimsPrincipal.Current.IsInRole("Contributor")
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            return database.Where(t => t.UserId == userId).OrderBy(t => t.CreatedTime).Select(t => new TodoItem { Id = t.Id, Title = t.Title, CategoryId = t.CategoryId });
        }

        /// <summary>
        /// Creates a new todo item for the current user.
        /// </summary>
        public async Task<IHttpActionResult> Post(TodoItemCreate value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Title))
            {
                return BadRequest("Title is required");
            }
            if (value == null || string.IsNullOrWhiteSpace(value.CategoryId))
            {
                return BadRequest("CategoryId is required");
            }

            // Create a new category via the Taxonomy Web API if requested.
            if (!string.IsNullOrWhiteSpace(value.NewCategoryName))
            {
                var client = await CategoryController.GetTaxonomyClient();
                var newCategory = new Category { Name = value.NewCategoryName, IsPrivate = value.NewCategoryIsPrivate };
                var newCategoryRequest = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.TaxonomyWebApiRootUrl + "api/category");
                newCategoryRequest.Content = new JsonContent(newCategory);
                var newCategoryResponse = await client.SendAsync(newCategoryRequest);
                newCategoryResponse.EnsureSuccessStatusCode();
                var newCategoryResponseString = await newCategoryResponse.Content.ReadAsStringAsync();
                var category = JsonConvert.DeserializeObject<Category>(newCategoryResponseString);
                value.CategoryId = category.Id;
            }

            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            var data = new TodoItemData(value.Title, value.CategoryId, userId);
            database.Add(data);
            return Ok();
        }
    }
}
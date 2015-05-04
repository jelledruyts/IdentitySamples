using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Http;
using TaxonomyWebApi.Models;

namespace TaxonomyWebApi.Controllers
{
    [Authorize]
    public class CategoryController : ApiController
    {
        #region In-Memory Database

        private static ConcurrentBag<CategoryData> database = new ConcurrentBag<CategoryData>() { new CategoryData("Home", null), new CategoryData("Work", null) };

        private class CategoryData
        {
            public string Id { get; set; }
            public DateTimeOffset CreatedTime { get; set; }
            public string Name { get; set; }
            public string UserId { get; set; }

            public CategoryData(string name, string userId)
            {
                this.Id = Guid.NewGuid().ToString();
                this.CreatedTime = DateTimeOffset.UtcNow;
                this.Name = name;
                this.UserId = userId;
            }
        }

        #endregion

        public IEnumerable<Category> Get()
        {
            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            return database.Where(c => c.UserId == null || c.UserId == userId).OrderBy(c => c.Name).Select(c => new Category { Id = c.Id, Name = c.Name, IsPrivate = c.UserId != null });
        }

        public IHttpActionResult Post(Category value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Name))
            {
                return BadRequest("Name is required");
            }
            var userId = value.IsPrivate ? ClaimsPrincipal.Current.GetUniqueIdentifier() : null;
            var data = new CategoryData(value.Name, userId);
            database.Add(data);
            value.Id = data.Id;
            return Ok(value);
        }
    }
}
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaxonomyWebApi.Models;

namespace TaxonomyWebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class CategoryController : Controller
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

        /// <summary>
        /// Gets all public categories as well as the current user's private categories.
        /// </summary>
        [HttpGet]
        public IEnumerable<Category> Get()
        {
            var userId = this.User.GetUniqueIdentifier();
            return database.Where(c => c.UserId == null || c.UserId == userId).OrderBy(c => c.Name).Select(c => new Category { Id = c.Id, Name = c.Name, IsPrivate = c.UserId != null });
        }

        /// <summary>
        /// Creates a new category.
        /// </summary>
        [HttpPost]
        public IActionResult Post([FromBody]Category value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Name))
            {
                return BadRequest("Name is required");
            }
            var userId = value.IsPrivate ? this.User.GetUniqueIdentifier() : null;
            var data = new CategoryData(value.Name, userId);
            database.Add(data);
            value.Id = data.Id;
            return Ok(value);
        }
    }
}
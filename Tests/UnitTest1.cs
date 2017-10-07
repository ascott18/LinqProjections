using EasyQuery;
using EasyQuery.Controllers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {

        [Fact]
        public void DbTest()
        {
            var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;Database=EasyQueryDb;Trusted_Connection=True;MultipleActiveResultSets=True;")
                .Options);

            context.Events.Add(new Event
            {
                Title = "Blah Event",
                DateCreated = DateTimeOffset.Now,
            });
            context.SaveChanges();

            var events = context
                .Events
                .ProjectProperties(e => e.EventId, e => e.Title)
                .ToList();

        }

        [Fact]
        public void ExpressionTest()
        {
            Expression<Func<Event, Event>> exp = e => new Event
            {
                EventId = e.EventId,
                Title = e.Title
            };
            LambdaExpression ee = exp;

            var exp2 = Enumerable
                    .Empty<Event>()
                    .AsQueryable()
                    .ProjectProperties(e => e.EventId, e => e.Title)
                    .Expression;

            var exp3 = Enumerable
                    .Empty<Event>()
                    .AsQueryable()
                    .Project(Members.Primitives)
                    .Expression;

            var exp4 = Enumerable
                    .Empty<Event>()
                    .AsQueryable()
                    .Project(Members.Primitives, e => new Event
                         {
                             EventNotes = e.EventNotes
                                .Where(n => n.Text != null)
                                .Project(Members.All, n => new EventNote
                                {
                                    Comments = n.Comments
                                        .Where(u => u.Text.Contains("Andrew") || e.EventId == 7)
                                        .ToList()
                                }).ToList()
                         }
                    )
                    .Expression;

            Console.WriteLine(exp4);
        }
    }
}

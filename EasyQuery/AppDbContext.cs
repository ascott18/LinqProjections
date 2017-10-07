using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EasyQuery
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }

        public DbSet<EventNote> EventNotes { get; set; }
    }

    public abstract class Auditable
    {


        [ForeignKey(nameof(CreatedByUser))]
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; }

        [ForeignKey(nameof(ModifiedByUser))]
        public int ModifiedByUserId { get; set; }
        public User ModifiedByUser { get; set; }

        public DateTimeOffset DateCreated { get; set; }

        public DateTimeOffset DateModified { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }


    public class Event : Auditable
    {
        public int EventId { get; set; }

        public ICollection<EventNote> EventNotes { get; set; }

        public string Title { get; set; }
    }

    public class EventNote : Auditable
    {
        public int EventNoteId { get; set; }

        public Event Event { get; set; }

        public int EventId { get; set; }

        public string Text { get; set; }

        public DateTimeOffset Date { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
    
    public class Comment
    {
        public int CommentId { get; set; }

        public string Text { get; set; }

        public EventNote Note { get; set; }

        public int EventNoteId { get; set; }
    }
}

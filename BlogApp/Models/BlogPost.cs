using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogApp.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Title { get; set; }

        [Required]
        public string? Content { get; set; }

        [Required, StringLength(100)]
        public string? Author { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? ImagePath { get; set; }
        public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [Required]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }   
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

    }
}

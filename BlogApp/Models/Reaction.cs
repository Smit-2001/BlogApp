using System.ComponentModel.DataAnnotations;

namespace BlogApp.Models
{
    public class Reaction
    {
        public int Id { get; set; }

        [Required]
        public int BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; }

        [Required]
        public string UserId { get; set; }  
        public ApplicationUser User { get; set; }

        [Required, StringLength(20)]
        public string Type { get; set; } 
    }
}

using System.ComponentModel.DataAnnotations;

namespace TodoListApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot be longer than 150 characters.")]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Deadline is required.")]
        [Display(Name = "Deadline")]
        public DateTime Deadline { get; set; }

        // Stores the path to the file saved on the server
        public string? FilePath { get; set; }

        // Stores the original file name for downloads
        public string? OriginalFileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

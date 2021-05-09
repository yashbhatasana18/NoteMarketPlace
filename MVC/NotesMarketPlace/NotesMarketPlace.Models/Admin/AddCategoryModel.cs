using System.ComponentModel.DataAnnotations;

namespace NotesMarketPlace.Models.Admin
{
    public class AddCategoryModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }
    }
}

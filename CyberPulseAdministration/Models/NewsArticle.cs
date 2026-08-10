using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CyberPulseAdministration.Models
{
    public class NewsArticle
    {
        public int ID { get; set; }

        [StringLength(255)]
        [Display(Name = "Image Name")]
        public string ImageName { get; set; }

        [StringLength(255)]
        [Display(Name = "Image Path")]
        public string ImagePath { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [StringLength(10)]
        public string Type { get; set; }

        [StringLength(60)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "URL is required.")]
        [StringLength(1024)]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string URL { get; set; }

        [JsonProperty("isActive")]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
}

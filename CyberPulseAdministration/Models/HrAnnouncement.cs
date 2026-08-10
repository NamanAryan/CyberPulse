using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace CyberPulseAdministration.Models
{
    public class HrAnnouncement
    {
        public int ID { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Title cannot exceed 50 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "No spaces and special characters allowed.")]
        public string Title { get; set; }

        [AllowHtml]
        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Short Description is required.")]
        [StringLength(500)]
        public string ShortDescription { get; set; }

        [Required(ErrorMessage = "Page Title is required.")]
        [StringLength(200)]
        [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "No spaces and special characters allowed.")]
        public string PageTitle { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        public Guid? AnnouncementGuid { get; set; }
    }
}

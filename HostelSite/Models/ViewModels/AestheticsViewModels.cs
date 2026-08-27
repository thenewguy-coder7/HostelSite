using System.ComponentModel.DataAnnotations;

namespace HostelSite.ViewModels
{
    // ── SUBMIT REQUEST (from Aesthetics/Index form) ──
    public class AestheticRequestViewModel
    {
        [Required]
        public string StyleName { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hostel name is required")]
        public string Hostel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Room number is required")]
        public string RoomNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a preferred setup date")]
        [DataType(DataType.Date)]
        public DateTime PreferredDate { get; set; }

        public string? Notes { get; set; }
    }

    // ── MY REQUESTS list item ──
    public class AestheticRequestSummaryViewModel
    {
        public int Id { get; set; }
        public string StyleName { get; set; } = string.Empty;
        public string Hostel { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? PreferredDate { get; set; }
        public string? Notes { get; set; }
    }
}

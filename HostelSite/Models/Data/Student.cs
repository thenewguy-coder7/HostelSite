using System;
using System.Collections.Generic;

namespace HostelSite.Models.Data;

public partial class Student
{
    public int StudentId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string StudentNumber { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateOnly? EnrollmentDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AestheticRequest> AestheticRequests { get; set; } = new List<AestheticRequest>();


    public virtual ICollection<LogisticsOrder> LogisticsOrders { get; set; } = new List<LogisticsOrder>();
}

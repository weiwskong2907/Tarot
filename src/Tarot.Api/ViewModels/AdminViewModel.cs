namespace Tarot.Api.ViewModels;

public class AdminDashboardViewModel
{
    public int PendingOrders { get; set; }
    public int TodaysAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<AdminTaskViewModel> Tasks { get; set; } = new();
}

public class AdminTaskViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string DueTime { get; set; } = string.Empty;
}

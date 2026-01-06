namespace Tarot.Api.ViewModels;

public class DashboardViewModel
{
    public int LoyaltyPoints { get; set; }
    public List<AppointmentViewModel> UpcomingAppointments { get; set; } = new();
    public List<AppointmentViewModel> RecentHistory { get; set; } = new();
}

public class AppointmentViewModel
{
    public string ServiceName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}

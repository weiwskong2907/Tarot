using System.ComponentModel.DataAnnotations;
using Tarot.Api.ViewModels;

namespace Tarot.Api.ViewModels;

public class BookingViewModel
{
    [Required]
    public Guid ServiceId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Required]
    public DateTime StartTime { get; set; } // Time of day

    public List<ServiceViewModel> AvailableServices { get; set; } = new();
    
    public ServiceViewModel? SelectedService { get; set; }
}

public class SlotViewModel
{
    public string Time { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

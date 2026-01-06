namespace Tarot.Api.ViewModels;

public class HomeViewModel
{
    public string Title { get; set; } = string.Empty;
    public List<ServiceViewModel> FeaturedServices { get; set; } = new();
}

public class ServiceViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Duration { get; set; } = string.Empty;
}

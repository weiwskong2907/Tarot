using System.ComponentModel.DataAnnotations;

namespace Tarot.Api.Dtos;

public class AiInterpretationRequestDto
{
    [Required]
    public string SpreadType { get; set; } = string.Empty;
    
    [Required]
    public List<Guid> CardIds { get; set; } = [];
    
    [Required]
    public string Question { get; set; } = string.Empty;
}

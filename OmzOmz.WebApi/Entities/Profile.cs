using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OmzOmz.WebApi.StateMachines;

namespace OmzOmz.WebApi.Entities;

public class Profile
{
    /// <summary>
    /// Telegram Chat ID
    /// </summary>
    public required long Id { get; set; }

    [MaxLength(512)] 
    public required string Name { get; set; }
    
    [MaxLength(1024)] 
    public required string Description { get; set; }
    
    public Chat Chat { get; set; }

    public bool IsOnboarded => !string.IsNullOrWhiteSpace(Description) && !string.IsNullOrWhiteSpace(Name);
}
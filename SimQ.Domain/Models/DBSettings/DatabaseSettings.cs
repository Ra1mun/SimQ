namespace SimQ.Domain.Models.DBSettings;

public record DatabaseSettings
{
    public string ConnectionString { get; init; }
    
    public string DatabaseName { get; init; }
}
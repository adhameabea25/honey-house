namespace HoneyHouse.Infrastructure.Persistence.Seed;

public class SeedOptions
{
    public const string Section = "Seed";
    public bool ApplyMigrations { get; set; } = true;
    public bool SampleData { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string AdminFullName { get; set; } = "Honey House Admin";
}

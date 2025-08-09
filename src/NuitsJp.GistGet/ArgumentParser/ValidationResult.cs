namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// Represents the result of command argument validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    
    public static ValidationResult Success() => new() { IsValid = true };
    
    public static ValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };
    
    public static ValidationResult Failure(IEnumerable<string> errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };
    
    public ValidationResult WithWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }
    
    public ValidationResult WithWarnings(IEnumerable<string> warnings)
    {
        Warnings.AddRange(warnings);
        return this;
    }
}
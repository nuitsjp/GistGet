using System.CommandLine;

namespace NuitsJp.GistGet.ArgumentParser;

/// <summary>
/// Interface for WinGet-compatible argument parsing
/// Provides command structure and validation according to winget.exe specifications
/// </summary>
public interface IWinGetArgumentParser
{
    /// <summary>
    /// Builds the root command with all WinGet subcommands and options
    /// </summary>
    /// <returns>Configured root command for System.CommandLine</returns>
    Command BuildRootCommand();
    
    /// <summary>
    /// Validates command arguments according to WinGet specifications
    /// Includes mutual exclusivity, conditional requirements, etc.
    /// </summary>
    /// <param name="parseResult">Parse result from System.CommandLine</param>
    /// <returns>Validation result with errors if any</returns>
    ValidationResult ValidateArguments(ParseResult parseResult);
}
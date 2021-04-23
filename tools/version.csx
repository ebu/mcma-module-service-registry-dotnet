public enum Stage
{
    Alpha,
    Beta,
    RC
}

public class Version
{
    public Version(int major, int minor, int patch, Stage? preReleaseStage = null, int? preReleaseNumber = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreReleaseStage = preReleaseStage;
        PreReleaseNumber = preReleaseNumber;
    }
    
    public int Major { get; }
    
    public int Minor { get; }
    
    public int Patch { get; }
    
    public Stage? PreReleaseStage { get; }
    
    public int? PreReleaseNumber { get; }

    public static Version Parse(string version)
    {
        if (version == null) throw new ArgumentNullException(nameof(version));
        
        var releaseParts = version.Split(new[] {"-"}, StringSplitOptions.RemoveEmptyEntries);
        var preReleaseLabel = releaseParts.ElementAtOrDefault(1);
        version = releaseParts[0];
        
        var versionParts = version.Split(new []{'.'}, StringSplitOptions.RemoveEmptyEntries);
        if (versionParts.Length != 3)
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Must contain 3 parts delimited by periods.");
            
        if (!int.TryParse(versionParts[0], out var major))
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Major value '{versionParts[0]}' must be an integer value.");
        if (!int.TryParse(versionParts[1], out var minor))
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Minor value '{versionParts[1]}' must be an integer value.");
        if (!int.TryParse(versionParts[2], out var patch))
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Patch value '{versionParts[2]}' must be an integer value.");
            
        if (preReleaseLabel == null)
            return new Version(major, minor, patch);
            
        Stage? preReleaseStage = null;
        
        string preReleaseNumberText = null;
        if (preReleaseLabel.StartsWith(Stage.Alpha.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            preReleaseStage = Stage.Alpha;
            preReleaseNumberText = preReleaseLabel.Substring(Stage.Alpha.ToString().Length);
        }
        else if (preReleaseLabel.StartsWith(Stage.Beta.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            preReleaseStage = Stage.Beta;
            preReleaseNumberText = preReleaseLabel.Substring(Stage.Beta.ToString().Length);
        }
        else if (preReleaseLabel.StartsWith(Stage.RC.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            preReleaseStage = Stage.RC;
            preReleaseNumberText = preReleaseLabel.Substring(Stage.RC.ToString().Length);
        }
            
        if (!preReleaseStage.HasValue)
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Pre-release label '{preReleaseLabel}' does not start with a known pre-release stage ('alpha', 'beta', or 'rc').");
            
        if (!int.TryParse(preReleaseNumberText, out var preReleaseNumber))
            throw new ArgumentException(nameof(version), $"Invalid semantic version '{version}'. Pre-release number '{preReleaseNumberText}' must be an integer value.");
        
        return new Version(major, minor, patch, preReleaseStage, preReleaseNumber);
    }
    
    public static Version Initial()
        => new Version(0, 0, 1, Stage.Alpha, 1); 
        
    public static Version FromArgsOrFile(string[] args, string file, bool autoIncrement = true)
    {
        var fromArgs = FromArgs(args);
        if (fromArgs != null)
            return fromArgs;

        if (!File.Exists("version"))
            return Version.Initial();
        
        try
        {
            var parsed = Version.Parse(File.ReadAllText("version"));
            return autoIncrement ? parsed.Next() : parsed;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to read version from local 'version' file", ex);
        }
    }
    
    public static Version FromArgs(string[] args)
    { 
        var versionArg =
            args?.FirstOrDefault(x => x.StartsWith("-v", StringComparison.OrdinalIgnoreCase) || x.StartsWith("--version", StringComparison.OrdinalIgnoreCase));
        if (versionArg == null)
            return null;
    
        var versionText = default(string);
        
        var versionArgParts = versionArg.Split("=");
        if (versionArgParts.Length > 2)
            throw new Exception($"Invalid version argument '{string.Join("=", versionArgParts.Skip(1))}'");
        
        if (versionArgParts.Length == 1)
        {
            var versionArgIndex = Array.IndexOf(args, versionArg);
            versionText = args.ElementAtOrDefault(versionArgIndex + 1);
        }
        else
            versionText = versionArgParts[1];
        
        return Version.Parse(versionText);
    }
    
    public Version Next()
        => PreReleaseStage.HasValue ? new Version(Major, Minor, Patch, PreReleaseStage, PreReleaseNumber + 1) : new Version(Major, Minor, Patch + 1);
    
    public override string ToString()
        => $"{Major}.{Minor}.{Patch}{(PreReleaseStage.HasValue ? $"-{PreReleaseStage.Value.ToString().ToLower()}{PreReleaseNumber}" : string.Empty)}";
}
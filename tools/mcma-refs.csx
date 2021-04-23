#load "./version.csx"

using System.Text.RegularExpressions;

private static int RunForEachProject(Action<string, string, string> run)
{
    foreach (var csprojFile in Directory.GetFiles(".", "*.csproj", SearchOption.AllDirectories))
        run(Path.GetDirectoryName(csprojFile), Path.GetFileNameWithoutExtension(csprojFile), csprojFile);
      
    return 0;
}

public static void SetMcmaVersion(string version)
{
    var parsedVersion = Version.Parse(version);  

    RunForEachProject((projectFolder, projectName, csprojFile) => 
        File.WriteAllText(
            csprojFile,
            Regex.Replace(
                File.ReadAllText(csprojFile),
                @"(\<PackageReference\s+Include=""Mcma.+""\s+Version=)""\d+\.\d+\.\d+(?:-(?:alpha|beta|rc)\d+)?""(\s+\/\>)",
                "$1\"" + parsedVersion.ToString() + "\"$2")));
}
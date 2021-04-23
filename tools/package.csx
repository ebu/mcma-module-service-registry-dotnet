#r "nuget:Newtonsoft.Json, 12.0.3"

#load "./aws.csx"
#load "./azure.csx"
#load "./version.csx"

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var ProviderPackagers = new Dictionary<string, Func<string, string, Task>>(StringComparer.OrdinalIgnoreCase);
ProviderPackagers["aws"] = PackageLambdaAsync;
ProviderPackagers["azure"] = PackageFunctionAppAsync;

private JObject GetJsonFromFile(string root, string fileName)
{
    var path = Path.Combine(root, fileName);
    if (!File.Exists(path))
        throw new Exception($"Unable to package module. {fileName} not found in {root}");

    try
    {
        return JObject.Parse(File.ReadAllText(path));
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to parse json from {path}", ex);
    }
}

private async Task<string> PackageModuleAsync(string moduleFolder, Version version)
{
    var outputFolder = Path.Combine(moduleFolder, ".publish", version.ToString(), "staging");
    try
    {
        var moduleJson = GetJsonFromFile(moduleFolder, "module.json");
        var modulePackageJson = GetJsonFromFile(moduleFolder, "module-package.json");
        
        var provider = moduleJson["provider"].Value<string>();
        if (!ProviderPackagers.ContainsKey(provider))
            throw new Exception($"No packager found for provider {provider}");
        
        var providerPackager = ProviderPackagers[provider];
        
        var functions = modulePackageJson["functions"].ToObject<Dictionary<string, string>>();
        
        foreach (var function in functions)
        {
            var functionSrcFolder = Path.Combine(moduleFolder, function.Value);
            var functionZipFile = Path.Combine(outputFolder, "functions", function.Key + ".zip");
            
            await providerPackager(functionSrcFolder, functionZipFile);
        }
        
        var additionalFiles = modulePackageJson["additionalFiles"];
        if (additionalFiles != null)
        {
            foreach (var additionalFile in additionalFiles)
            {
                string src, dest;
                switch (additionalFile.Type)
                {
                    case JTokenType.Object:
                        src = additionalFile[nameof(src)].Value<string>();
                        dest = additionalFile[nameof(dest)].Value<string>();
                        break;
                    case JTokenType.String:
                        src = additionalFile.Value<string>();
                        dest = additionalFile.Value<string>();
                        break;
                    default:
                        throw new Exception("Invalid value in 'additionalFiles'. Value must be an object or a string.");
                }
                
                var destPath = Path.Combine(outputFolder, dest);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(Path.Combine(moduleFolder, src), destPath, true);
            }
        }
        
        moduleJson["version"] = version.ToString();
        File.WriteAllText(Path.Combine(outputFolder, "module.json"), moduleJson.ToString(Formatting.Indented));
        
        foreach (var tfFile in Directory.EnumerateFiles(moduleFolder, "*.tf"))
            File.Copy(tfFile, Path.Combine(outputFolder, Path.GetFileName(tfFile)), true);
            
        return outputFolder;
    }
    catch
    {
        try
        {
            Directory.Delete(outputFolder, true);
        }
        catch
        {
        }
        throw;
    }
}

public Task<string> PackageModuleAsync(string moduleFolder, string[] args)
    => PackageModuleAsync(moduleFolder, Version.FromArgsOrFile(args, "version"));

public async Task PackageAllModulesAsync(string[] args)
{
    var parsedVersion = Version.FromArgsOrFile(args, "version");
    
    var outputFolders = new List<string>();
    try
    {
        foreach (var moduleFile in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "module.json", SearchOption.AllDirectories))
        {
            if (moduleFile.IndexOf(".publish", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
                
            var moduleFolder = Path.GetDirectoryName(moduleFile);
            outputFolders.Add(await PackageModuleAsync(moduleFolder, parsedVersion));
        }
            
        File.WriteAllText("version", parsedVersion.ToString());
    }
    catch
    {
        foreach (var outputFolder in outputFolders)
        {
            try
            {
                Directory.Delete(outputFolder, true);
            }
            catch
            {
            }
        }
        
        throw;
    }
}
#r "nuget:Mcma.Aws.Client, 0.14-rc4"
#r "nuget:AWSSDK.Core, 3.5.3.8"

#load "./package.csx"

using System.IO.Compression;
using System.Net.Http;
using Amazon.Runtime.CredentialManagement;
using Mcma.Aws.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const string ModuleRepUrl = "https://3lampy8hqj.execute-api.us-east-1.amazonaws.com/prod/";

var sharedCredentialsFile = new SharedCredentialsFile();
var httpClient = new HttpClient();

private async Task<Aws4Signer> GetRequestSignerForProfileAsync(string profileName)
{
    if (!sharedCredentialsFile.TryGetProfile(profileName, out var credentialProfile))
        throw new Exception($"Unknown AWS profile '{profileName}'.");

    var credentialsWrapper = credentialProfile.GetAWSCredentials(sharedCredentialsFile);
    var credentials = await credentialsWrapper.GetCredentialsAsync();
    
    return new Aws4Signer(credentials.AccessKey, credentials.SecretKey, credentialProfile.Region.SystemName, credentials.Token);
}

private async Task<string> GetPublishUrlAsync(string moduleJson, Aws4Signer awsSigner)
{   
    var request = new HttpRequestMessage(HttpMethod.Post, $"{ModuleRepUrl}modules/publish")
    {
        Content = new StringContent(moduleJson, Encoding.UTF8, "application/json")
    };
    
    await awsSigner.SignAsync(request);
    
    var publishResp = await httpClient.SendAsync(request);
    if (!publishResp.IsSuccessStatusCode)
    {
        var errorMessage = $"Response status code does not indicate success: {(int)publishResp.StatusCode} ({publishResp.ReasonPhrase})";
        try
        {
            var errorBody = await publishResp.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(errorBody))
            {
                try
                {
                    errorBody = JObject.Parse(errorBody).ToString(Formatting.Indented);
                }
                catch
                {
                }
                errorMessage += Environment.NewLine + "Body:" + Environment.NewLine + errorBody;
            }
        }
        finally
        {
            if (publishResp.Content is IDisposable disposableContent)
                disposableContent.Dispose();
        }
        
        throw new HttpRequestException(errorMessage);
    }
    
    var publishRespJson = JObject.Parse(await publishResp.Content.ReadAsStringAsync());
    
    return publishRespJson["publishUrl"].Value<string>();
}

private async Task UploadZipAsync(string moduleFolder, string outputFolder, string publishUrl, Version version)
{
    var moduleZipFilePath = Path.Combine(moduleFolder, ".publish", version.ToString(), $"{version}.zip"); 
    
    if (File.Exists(moduleZipFilePath))
        File.Delete(moduleZipFilePath);
        
    ZipFile.CreateFromDirectory(outputFolder, moduleZipFilePath);
    
    using var uploadStream = File.OpenRead(moduleZipFilePath); 
    var uploadResp = await httpClient.PutAsync(publishUrl, new StreamContent(uploadStream));
    uploadResp.EnsureSuccessStatusCode();
}

private async Task PublishModuleAsync(string moduleFolder, Version version)
{
    var outputFolder = await PackageModuleAsync(moduleFolder, version);
    
    var moduleJson = File.ReadAllText(Path.Combine(outputFolder, "module.json"));
    
    var awsSigner = await GetRequestSignerForProfileAsync("default");
    
    var publishUrl = await GetPublishUrlAsync(moduleJson, awsSigner);
    
    await UploadZipAsync(moduleFolder, outputFolder, publishUrl, version);
}

public Task PublishModuleAsync(string moduleFolder, string[] args)
    => PublishModuleAsync(moduleFolder, Version.FromArgsOrFile(args, "version"));

public async Task PublishAllModulesAsync(string[] args)
{
    var parsedVersion = Version.FromArgsOrFile(args, "version");
    
    foreach (var moduleFile in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "module.json", SearchOption.AllDirectories))
    {
        if (moduleFile.IndexOf(".publish", StringComparison.OrdinalIgnoreCase) >= 0)
            continue;
            
        var moduleFolder = Path.GetDirectoryName(moduleFile);
        await PublishModuleAsync(moduleFolder, parsedVersion);
    }
        
    File.WriteAllText("version", parsedVersion.ToString());
}
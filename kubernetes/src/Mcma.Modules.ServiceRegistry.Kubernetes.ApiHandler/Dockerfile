FROM mcr.microsoft.com/dotnet/aspnet:3.1

COPY bin/Release/netcoreapp3.1/linux-x64/publish/ app/
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "Mcma.Modules.ServiceRegistry.Kubernetes.ApiHandler.dll"]
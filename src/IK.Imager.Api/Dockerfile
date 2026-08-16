#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# .NET 8+ container images run as a non-root user and listen on 8080 by default
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Directory.Build.props carries TargetFramework and Directory.Packages.props carries every package
# version (Central Package Management). Without both, restore has no framework to restore for and
# every versionless PackageReference fails - so they have to land before the restore, not with COPY . .
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["IK.Imager.Api/IK.Imager.Api.csproj", "IK.Imager.Api/"]
COPY ["IK.Imager.Core/IK.Imager.Core.csproj", "IK.Imager.Core/"]
COPY ["IK.Imager.Api.Contract/IK.Imager.Api.Contract.csproj", "IK.Imager.Api.Contract/"]
COPY ["IK.Imager.Core.Abstractions/IK.Imager.Core.Abstractions.csproj", "IK.Imager.Core.Abstractions/"]
COPY ["IK.Imager.Storage.AzureBlobs/IK.Imager.Storage.AzureBlobs.csproj", "IK.Imager.Storage.AzureBlobs/"]
COPY ["IK.Imager.Storage.Abstractions/IK.Imager.Storage.Abstractions.csproj", "IK.Imager.Storage.Abstractions/"]
COPY ["IK.Imager.Storage.CosmosDb/IK.Imager.Storage.CosmosDb.csproj", "IK.Imager.Storage.CosmosDb/"]
RUN dotnet restore "IK.Imager.Api/IK.Imager.Api.csproj"
COPY . .
WORKDIR "/src/IK.Imager.Api"
RUN dotnet build "IK.Imager.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IK.Imager.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IK.Imager.Api.dll"]
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:5.0 AS build
WORKDIR /src
COPY ["IK.Imager.Api/IK.Imager.Api.csproj", "IK.Imager.Api/"]
COPY ["IK.Imager.EventBus.AzureServiceBus/IK.Imager.EventBus.AzureServiceBus.csproj", "IK.Imager.EventBus.AzureServiceBus/"]
COPY ["IK.Imager.EventBus.Abstractions/IK.Imager.EventBus.Abstractions.csproj", "IK.Imager.EventBus.Abstractions/"]
COPY ["IK.Imager.Core/IK.Imager.Core.csproj", "IK.Imager.Core/"]
COPY ["IK.Imager.IntegrationEvents/IK.Imager.IntegrationEvents.csproj", "IK.Imager.IntegrationEvents/"]
COPY ["IK.Imager.Core.Abstractions/IK.Imager.Core.Abstractions.csproj", "IK.Imager.Core.Abstractions/"]
COPY ["IK.Imager.Utils/IK.Imager.Utils.csproj", "IK.Imager.Utils/"]
COPY ["IK.Imager.ImageBlobStorage.AzureFiles/IK.Imager.ImageBlobStorage.AzureFiles.csproj", "IK.Imager.ImageBlobStorage.AzureFiles/"]
COPY ["IK.Imager.Storage.Abstractions/IK.Imager.Storage.Abstractions.csproj", "IK.Imager.Storage.Abstractions/"]
COPY ["IK.Imager.ImageMetadataStorage.CosmosDB/IK.Imager.ImageMetadataStorage.CosmosDB.csproj", "IK.Imager.ImageMetadataStorage.CosmosDB/"]
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
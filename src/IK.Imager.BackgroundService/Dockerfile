#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-nanoserver-1809 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-nanoserver-1809 AS build
WORKDIR /src
COPY ["IK.Imager.BackgroundService/IK.Imager.BackgroundService.csproj", "IK.Imager.BackgroundService/"]
COPY ["IK.Imager.EventBus.AzureServiceBus/IK.Imager.EventBus.AzureServiceBus.csproj", "IK.Imager.EventBus.AzureServiceBus/"]
COPY ["IK.Imager.EventBus.Abstractions/IK.Imager.EventBus.Abstractions.csproj", "IK.Imager.EventBus.Abstractions/"]
COPY ["IK.Imager.Core/IK.Imager.Core.csproj", "IK.Imager.Core/"]
COPY ["IK.Imager.Core.Abstractions/IK.Imager.Core.Abstractions.csproj", "IK.Imager.Core.Abstractions/"]
COPY ["IK.Imager.Utils/IK.Imager.Utils.csproj", "IK.Imager.Utils/"]
COPY ["IK.Imager.ImageBlobStorage.AzureFiles/IK.Imager.ImageBlobStorage.AzureFiles.csproj", "IK.Imager.ImageBlobStorage.AzureFiles/"]
COPY ["IK.Imager.Storage.Abstractions/IK.Imager.Storage.Abstractions.csproj", "IK.Imager.Storage.Abstractions/"]
COPY ["IK.Imager.ImageMetadataStorage.CosmosDB/IK.Imager.ImageMetadataStorage.CosmosDB.csproj", "IK.Imager.ImageMetadataStorage.CosmosDB/"]
RUN dotnet restore "IK.Imager.BackgroundService/IK.Imager.BackgroundService.csproj"
COPY . .
WORKDIR "/src/IK.Imager.BackgroundService"
RUN dotnet build "IK.Imager.BackgroundService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IK.Imager.BackgroundService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IK.Imager.BackgroundService.dll"]
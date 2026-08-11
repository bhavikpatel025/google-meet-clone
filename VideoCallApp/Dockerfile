# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all the csproj files and restore as distinct layers
COPY ["VideoCallApp.API/VideoCallApp.API.csproj", "VideoCallApp.API/"]
COPY ["VideoCallApp.Application/VideoCallApp.Application.csproj", "VideoCallApp.Application/"]
COPY ["VideoCallApp.Domain/VideoCallApp.Domain.csproj", "VideoCallApp.Domain/"]
COPY ["VideoCallApp.Infrastructure/VideoCallApp.Infrastructure.csproj", "VideoCallApp.Infrastructure/"]
COPY ["VideoCallApp.Persistence/VideoCallApp.Persistence.csproj", "VideoCallApp.Persistence/"]

RUN dotnet restore "./VideoCallApp.API/VideoCallApp.API.csproj"

# Copy the rest of the code and build
COPY . .
WORKDIR "/src/VideoCallApp.API"
RUN dotnet build "./VideoCallApp.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoCallApp.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoCallApp.API.dll"]

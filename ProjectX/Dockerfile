# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ARG APP_UID=1001
RUN useradd -u $APP_UID appuser
USER appuser
WORKDIR /app
EXPOSE 7069
EXPOSE 5064

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Chỉ copy csproj để tận dụng cache
COPY ["ProjectX/*.csproj", "ProjectX/"]
RUN dotnet restore "ProjectX/ProjectX.csproj"

# Copy toàn bộ source code sau khi restore xong
COPY . .
WORKDIR "/src/ProjectX"

# Build
RUN dotnet build "ProjectX.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "ProjectX.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectX.dll"]

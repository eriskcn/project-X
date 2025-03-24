# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app

# Cài đặt dotnet-ef
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Sao chép file dự án và khôi phục dependencies
COPY *.csproj ./
RUN dotnet restore

# Sao chép mã nguồn
COPY . ./

# Build ứng dụng
RUN dotnet build -c $BUILD_CONFIGURATION --no-restore -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION --no-restore -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Cài đặt lại dotnet-ef trong stage final
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Sao chép mã nguồn gốc (bao gồm .csproj và thư mục Migrations) để chạy migrations
COPY . .

# Sao chép ứng dụng đã publish
COPY --from=publish /app/publish .

# Định nghĩa entrypoint để chạy ứng dụng
ENTRYPOINT ["dotnet", "ProjectX.dll"]
# Base runtime image
# dotnet ef migrations add init
# dotnet ef database update --project /app/ProjectX.csproj
# /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P TLU@42742hoai -C -Q "USE project_x"
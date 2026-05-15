FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["JobApplicationAssistant.Api.csproj", "./"]
RUN dotnet restore "./JobApplicationAssistant.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "JobApplicationAssistant.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JobApplicationAssistant.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official Playwright image which has all browsers and OS deps pre-installed.
# This avoids the SIGSYS (131) error during build-time installation.
FROM mcr.microsoft.com/playwright:v1.50.0-jammy AS final
WORKDIR /app

# Install .NET 10 Runtime
USER root
RUN apt-get update && apt-get install -y curl \
    && curl -sL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet-install.sh

# Copy the published application
COPY --from=publish /app/publish .

# Playwright in this base image stores browsers in /ms-playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENV ASPNETCORE_URLS=http://+:8080

# Create directories for persistence
RUN mkdir -p /app/wwwroot/screenshots && chmod 777 /app/wwwroot/screenshots

# Ensure the database file is writable
RUN touch job_assistant.db && chmod 777 job_assistant.db

ENTRYPOINT ["dotnet", "JobApplicationAssistant.Api.dll"]

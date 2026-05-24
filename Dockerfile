FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files to cache restore
COPY ["src/VenEl.MCPAssistant.Server/VenEl.MCPAssistant.Server.csproj", "src/VenEl.MCPAssistant.Server/"]
COPY ["src/VenEl.MCPAssistant.Core/VenEl.MCPAssistant.Core.csproj", "src/VenEl.MCPAssistant.Core/"]
COPY ["src/VenEl.MCPAssistant.Azure/VenEl.MCPAssistant.Azure.csproj", "src/VenEl.MCPAssistant.Azure/"]
COPY ["src/VenEl.MCPAssistant.Logging/VenEl.MCPAssistant.Logging.csproj", "src/VenEl.MCPAssistant.Logging/"]
COPY ["src/VenEl.MCPAssistant.Docker/VenEl.MCPAssistant.Docker.csproj", "src/VenEl.MCPAssistant.Docker/"]
COPY ["src/VenEl.MCPAssistant.GitHub/VenEl.MCPAssistant.GitHub.csproj", "src/VenEl.MCPAssistant.GitHub/"]
COPY ["src/VenEl.MCPAssistant.MSSql/VenEl.MCPAssistant.MSSql.csproj", "src/VenEl.MCPAssistant.MSSql/"]
COPY ["src/VenEl.MCPAssistant.Atlassian/VenEl.MCPAssistant.Atlassian.csproj", "src/VenEl.MCPAssistant.Atlassian/"]
COPY ["src/VenEl.MCPAssistant.LocalOffice/VenEl.MCPAssistant.LocalOffice.csproj", "src/VenEl.MCPAssistant.LocalOffice/"]
COPY ["src/VenEl.MCPAssistant.Slack/VenEl.MCPAssistant.Slack.csproj", "src/VenEl.MCPAssistant.Slack/"]
COPY ["src/VenEl.MCPAssistant.Kubernetes/VenEl.MCPAssistant.Kubernetes.csproj", "src/VenEl.MCPAssistant.Kubernetes/"]
COPY ["src/VenEl.MCPAssistant.AWS/VenEl.MCPAssistant.AWS.csproj", "src/VenEl.MCPAssistant.AWS/"]
COPY ["src/VenEl.MCPAssistant.GCP/VenEl.MCPAssistant.GCP.csproj", "src/VenEl.MCPAssistant.GCP/"]
COPY ["src/VenEl.MCPAssistant.Databricks/VenEl.MCPAssistant.Databricks.csproj", "src/VenEl.MCPAssistant.Databricks/"]
COPY ["src/VenEl.MCPAssistant.Bitwarden/VenEl.MCPAssistant.Bitwarden.csproj", "src/VenEl.MCPAssistant.Bitwarden/"]

RUN dotnet restore "src/VenEl.MCPAssistant.Server/VenEl.MCPAssistant.Server.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/VenEl.MCPAssistant.Server"
RUN dotnet publish "VenEl.MCPAssistant.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The MCP server communicates over stdio
ENTRYPOINT ["dotnet", "VenEl.MCPAssistant.Server.dll"]

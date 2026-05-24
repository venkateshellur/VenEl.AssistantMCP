FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files to cache restore
COPY ["src/VenEl.AssistantMCP.Server/VenEl.AssistantMCP.Server.csproj", "src/VenEl.AssistantMCP.Server/"]
COPY ["src/VenEl.AssistantMCP.Core/VenEl.AssistantMCP.Core.csproj", "src/VenEl.AssistantMCP.Core/"]
COPY ["src/VenEl.AssistantMCP.Azure/VenEl.AssistantMCP.Azure.csproj", "src/VenEl.AssistantMCP.Azure/"]
COPY ["src/VenEl.AssistantMCP.Logging/VenEl.AssistantMCP.Logging.csproj", "src/VenEl.AssistantMCP.Logging/"]
COPY ["src/VenEl.AssistantMCP.Docker/VenEl.AssistantMCP.Docker.csproj", "src/VenEl.AssistantMCP.Docker/"]
COPY ["src/VenEl.AssistantMCP.GitHub/VenEl.AssistantMCP.GitHub.csproj", "src/VenEl.AssistantMCP.GitHub/"]
COPY ["src/VenEl.AssistantMCP.MSSql/VenEl.AssistantMCP.MSSql.csproj", "src/VenEl.AssistantMCP.MSSql/"]
COPY ["src/VenEl.AssistantMCP.Atlassian/VenEl.AssistantMCP.Atlassian.csproj", "src/VenEl.AssistantMCP.Atlassian/"]
COPY ["src/VenEl.AssistantMCP.LocalOffice/VenEl.AssistantMCP.LocalOffice.csproj", "src/VenEl.AssistantMCP.LocalOffice/"]
COPY ["src/VenEl.AssistantMCP.Slack/VenEl.AssistantMCP.Slack.csproj", "src/VenEl.AssistantMCP.Slack/"]
COPY ["src/VenEl.AssistantMCP.Kubernetes/VenEl.AssistantMCP.Kubernetes.csproj", "src/VenEl.AssistantMCP.Kubernetes/"]
COPY ["src/VenEl.AssistantMCP.AWS/VenEl.AssistantMCP.AWS.csproj", "src/VenEl.AssistantMCP.AWS/"]
COPY ["src/VenEl.AssistantMCP.GCP/VenEl.AssistantMCP.GCP.csproj", "src/VenEl.AssistantMCP.GCP/"]
COPY ["src/VenEl.AssistantMCP.Databricks/VenEl.AssistantMCP.Databricks.csproj", "src/VenEl.AssistantMCP.Databricks/"]
COPY ["src/VenEl.AssistantMCP.Bitwarden/VenEl.AssistantMCP.Bitwarden.csproj", "src/VenEl.AssistantMCP.Bitwarden/"]

RUN dotnet restore "src/VenEl.AssistantMCP.Server/VenEl.AssistantMCP.Server.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/src/VenEl.AssistantMCP.Server"
RUN dotnet publish "VenEl.AssistantMCP.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# The MCP server communicates over stdio
ENTRYPOINT ["dotnet", "VenEl.AssistantMCP.Server.dll"]

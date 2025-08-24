# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Render will set $PORT automatically
ENV ASPNETCORE_URLS=http://+:${PORT}

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FQSAPI/FQSAPI.csproj", "FQSAPI/"]
RUN dotnet restore "FQSAPI/FQSAPI.csproj"
COPY . .
WORKDIR "/src/FQSAPI"
RUN dotnet build "FQSAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "FQSAPI.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FQSAPI.dll"]

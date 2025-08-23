# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the project files
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use the official .NET 9 ASP.NET runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy the published files from the build stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "SQLAPI.dll"]
# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /app


# Copy the prerequisite project files and restore dependencies
COPY cddo-users.models/*.csproj ./cddo-users.models/
COPY cddo-users/*.csproj ./cddo-users/

# Restore all project dependencies
RUN dotnet restore ./cddo-users.models/*.csproj
RUN dotnet restore ./cddo-users/*.csproj

# Copy the entire source code of the prerequisite projects
COPY cddo-users.models/. ./cddo-users.models/
COPY cddo-users/. ./cddo-users/

# Build and publish the prerequisite projects
RUN dotnet publish ./cddo-users.models/*.csproj -c Release -o /app/publish/cddo-users.models

# Build and publish the main project
RUN dotnet publish ./cddo-users/*.csproj -c Release -o /app/publish/cddo-users

# Use the official .NET runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime


# Set the working directory inside the container
WORKDIR /app

# Copy the build output from the build stage
COPY --from=build /app/publish/cddo-users.models ./
COPY --from=build /app/publish/cddo-users ./

# Expose the port that your application will run on
EXPOSE 8080 

# Set the entry point to run your application
ENTRYPOINT ["dotnet", "cddo-users.dll"]

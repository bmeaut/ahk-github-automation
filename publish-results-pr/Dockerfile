# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy files, restore, publish
COPY ./PublishResult.csproj ./
RUN dotnet restore PublishResult.csproj
COPY ./. ./
RUN dotnet publish PublishResult.csproj -c Release -o out

# Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy the published output
COPY --from=build /app/out /src
CMD ["dotnet", "/src/PublishResult.dll"]

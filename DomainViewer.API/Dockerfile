FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY DomainViewer.Core/DomainViewer.Core.csproj DomainViewer.Core/
COPY DomainViewer.Infrastructure/DomainViewer.Infrastructure.csproj DomainViewer.Infrastructure/
COPY DomainViewer.API/DomainViewer.API.csproj DomainViewer.API/
RUN dotnet restore DomainViewer.API/DomainViewer.API.csproj
COPY . .
RUN dotnet publish DomainViewer.API/DomainViewer.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "DomainViewer.API.dll"]

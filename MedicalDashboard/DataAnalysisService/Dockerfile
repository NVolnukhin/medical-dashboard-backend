FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY /DataAnalysisService/ ./DataAnalysisService/
COPY /Shared/ ./Shared/

WORKDIR /src/DataAnalysisService
RUN dotnet restore DataAnalysisService.csproj

RUN dotnet publish DataAnalysisService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "DataAnalysisService.dll"] 
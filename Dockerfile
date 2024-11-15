FROM mcr.microsoft.com/dotnet/sdk:9.0.100-azurelinux3.0-arm64v8 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR "/src"
COPY . .
RUN dotnet restore "FakeAutodiscover.csproj"
RUN dotnet build "FakeAutodiscover.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FakeAutodiscover.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FakeAutodiscover.dll"]


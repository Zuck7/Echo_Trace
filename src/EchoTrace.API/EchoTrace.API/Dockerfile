FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["EchoTrace.API/EchoTrace.API.csproj", "EchoTrace.API/"]
COPY ["EchoTrace.Application/EchoTrace.Application.csproj", "EchoTrace.Application/"]
COPY ["EchoTrace.Domain/EchoTrace.Domain.csproj", "EchoTrace.Domain/"]
COPY ["EchoTrace.Infrastructure/EchoTrace.Infrastructure.csproj", "EchoTrace.Infrastructure/"]
RUN dotnet restore "EchoTrace.API/EchoTrace.API.csproj"

COPY . .
WORKDIR /src/EchoTrace.API
RUN dotnet publish "EchoTrace.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EchoTrace.API.dll"]

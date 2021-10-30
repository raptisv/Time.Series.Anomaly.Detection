FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 5002

# ML.NET dependencies
RUN \
  sed -i 's/ main/ main contrib non-free/' /etc/apt/sources.list && \
  apt-get update && \
  DEBIAN_FRONTEND=noninteractive apt-get install -y intel-mkl

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Graylog2Grafana.Web/Graylog2Grafana.Web.csproj", "Graylog2Grafana.Web/"]
COPY ["Time.Series.Anomaly.Detection.Data/Time.Series.Anomaly.Detection.Data.csproj", "Time.Series.Anomaly.Detection.Data/"]
COPY ["Graylog2Grafana/Graylog2Grafana.csproj", "Graylog2Grafana/"]
COPY ["Time.Series.Anomaly.Detection/Time.Series.Anomaly.Detection.csproj", "Time.Series.Anomaly.Detection/"]
RUN dotnet restore "Graylog2Grafana.Web/Graylog2Grafana.Web.csproj"
COPY . .
WORKDIR "/src/Graylog2Grafana.Web"
RUN dotnet build "Graylog2Grafana.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Graylog2Grafana.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Graylog2Grafana.Web.dll"]
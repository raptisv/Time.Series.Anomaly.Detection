FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
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
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NinosParaCristo.Domain/ NinosParaCristo.Domain/
COPY NinosParaCristo.Application/ NinosParaCristo.Application/
COPY NinosParaCristo.Infrastructure/ NinosParaCristo.Infrastructure/
COPY NinosParaCristo.API/ NinosParaCristo.API/
RUN dotnet publish NinosParaCristo.API/NinosParaCristo.API.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=10000
ENV TZ=America/Costa_Rica
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "NinosParaCristo.API.dll"]

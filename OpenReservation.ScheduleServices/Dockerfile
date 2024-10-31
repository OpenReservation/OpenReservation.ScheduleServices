FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
WORKDIR /src
COPY ["OpenReservation.ScheduleServices/OpenReservation.ScheduleServices.csproj", "OpenReservation.ScheduleServices/"]
RUN dotnet restore "OpenReservation.ScheduleServices/OpenReservation.ScheduleServices.csproj"
COPY . .
WORKDIR "/src/OpenReservation.ScheduleServices"
RUN dotnet publish "OpenReservation.ScheduleServices.csproj" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "OpenReservation.ScheduleServices.dll"]

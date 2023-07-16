FROM mcr.microsoft.com/dotnet/aspnet:8.0-preview AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-preview AS build
WORKDIR /src
COPY ["OpenReservation.ScheduleServices/OpenReservation.ScheduleServices.csproj", "OpenReservation.ScheduleServices/"]
RUN dotnet restore "OpenReservation.ScheduleServices/OpenReservation.ScheduleServices.csproj"
COPY . .
WORKDIR "/src/OpenReservation.ScheduleServices"
RUN dotnet build "OpenReservation.ScheduleServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenReservation.ScheduleServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenReservation.ScheduleServices.dll"]

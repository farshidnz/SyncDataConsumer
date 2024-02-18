FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /code
COPY accountsSyncDataConsumer.sln ./
COPY ./src ./src
COPY ./tests ./tests
RUN dotnet restore "accountsSyncDataConsumer.sln"
RUN dotnet build "accountsSyncDataConsumer.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "accountsSyncDataConsumer.sln" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountSyncData.Consumer.dll"]
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["RV.SB.Sender.ActivePassive/RV.SB.Sender.ActivePassive.csproj", "RV.SB.Sender.ActivePassive/"]

RUN dotnet restore "RV.SB.Sender.ActivePassive/RV.SB.Sender.ActivePassive.csproj"
COPY . .
WORKDIR "/src/RV.SB.Sender.ActivePassive"
RUN dotnet build "RV.SB.Sender.ActivePassive.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RV.SB.Sender.ActivePassive.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RV.SB.Sender.ActivePassive.dll"]
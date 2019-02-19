FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["RV.SB.Sender.ActiveActive/RV.SB.Sender.ActiveActive.csproj", "RV.SB.Sender.ActiveActive/"]

RUN dotnet restore "RV.SB.Sender.ActiveActive/RV.SB.Sender.ActiveActive.csproj"
COPY . .
WORKDIR "/src/RV.SB.Sender.ActiveActive"
RUN dotnet build "RV.SB.Sender.ActiveActive.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RV.SB.Sender.ActiveActive.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RV.SB.Sender.ActiveActive.dll"]
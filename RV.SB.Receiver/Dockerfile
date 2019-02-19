FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["RV.SB.Receiver/RV.SB.Receiver.csproj", "RV.SB.Receiver/"]

RUN dotnet restore "RV.SB.Receiver/RV.SB.Receiver.csproj"
COPY . .
WORKDIR "/src/RV.SB.Receiver"
RUN dotnet build "RV.SB.Receiver.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RV.SB.Receiver.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RV.SB.Receiver.dll"]
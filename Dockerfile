# ---------- build ----------
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /src
    COPY NuGet.config ./
    COPY BackEnd.csproj ./
    RUN dotnet restore BackEnd.csproj --verbosity:minimal
    COPY . .
    RUN dotnet publish BackEnd.csproj -c Release -o /app /p:UseAppHost=false
    
    # ---------- runtime ----------
    FROM mcr.microsoft.com/dotnet/aspnet:9.0
    WORKDIR /app
    COPY --from=build /app ./
    ENV ASPNETCORE_URLS=http://0.0.0.0:8080
    ENV ASPNETCORE_ENVIRONMENT=Production
    EXPOSE 8080
    ENTRYPOINT ["dotnet","BackEnd.dll"]
    
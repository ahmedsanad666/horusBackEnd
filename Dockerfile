# ---------- build ----------
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    WORKDIR /src
    COPY . .
    RUN dotnet restore ./BackEnd.csproj
    RUN dotnet publish ./BackEnd.csproj -c Release -o /app /p:UseAppHost=false
    
    # ---------- runtime ----------
    FROM mcr.microsoft.com/dotnet/aspnet:8.0
    WORKDIR /app
    COPY --from=build /app ./
    # listen on 8080 inside the container
    ENV ASPNETCORE_URLS=http://0.0.0.0:8080
    # set production by default
    ENV ASPNETCORE_ENVIRONMENT=Production
    EXPOSE 8080
    ENTRYPOINT ["dotnet","BackEnd.dll"]
    
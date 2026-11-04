# Multi-stage build: build stage + runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /build
COPY src/FlashSale.Worker/FlashSale.Worker.csproj ./FlashSale.Worker/
COPY src/FlashSale.Application/FlashSale.Application.csproj ./FlashSale.Application/
COPY src/FlashSale.Domain/FlashSale.Domain.csproj ./FlashSale.Domain/
COPY src/FlashSale.Infrastructure/FlashSale.Infrastructure.csproj ./FlashSale.Infrastructure/
RUN dotnet restore ./FlashSale.Worker/FlashSale.Worker.csproj
COPY . .
WORKDIR /build/src/FlashSale.Worker
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FlashSale.Worker.dll"]

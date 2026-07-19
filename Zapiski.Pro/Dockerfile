FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./

RUN dotnet restore \
    && dotnet publish -c Release -o out --no-restore \
    && rm -rf /root/.nuget/packages /root/.local/share/NuGet

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y \
    tzdata \
    libgssapi-krb5-2

ENV TZ=Europe/Moscow

COPY --from=build /app/out .
RUN rm -f .env

ENTRYPOINT ["dotnet", "Zapiski.Pro.dll"]

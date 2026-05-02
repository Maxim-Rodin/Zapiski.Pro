FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out
RUN apt-get update && apt-get install -y libgssapi-krb5-2

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/out .

ENTRYPOINT ["dotnet", "Zapiski.Pro.dll"]
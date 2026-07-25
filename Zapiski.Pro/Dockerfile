FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_XMLDOC_MODE=skip

COPY Zapiski.Pro.csproj ./
RUN dotnet restore --disable-parallel

COPY BasedClasses/ ./BasedClasses/
COPY CallBacks/ ./CallBacks/
COPY ClassMiniApp/ ./ClassMiniApp/
COPY Properties/ ./Properties/
COPY Services/ ./Services/
COPY State/ ./State/
COPY source/helloPicture.png ./source/helloPicture.png
COPY Class1.cs EnvConfig.cs Program.cs UserService.cs ./

RUN dotnet publish -c Release -o out --no-restore \
    && rm -rf /root/.nuget/packages /root/.local/share/NuGet

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    tzdata \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Europe/Moscow

COPY --from=build /app/out .
RUN rm -f .env

ENTRYPOINT ["dotnet", "Zapiski.Pro.dll"]

# install sdk
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ARG BUILD_CONFIGURATION=Release

# create sources folder
WORKDIR /src
# copy solution and project files
COPY *.sln ./
COPY CarInsuranceSalesBot/CarInsuranceSalesBot.csproj ./CarInsuranceSalesBot/

# restore dependencies
RUN dotnet restore

# copy everething
COPY . ./
WORKDIR /src/CarInsuranceSalesBot
# build project
RUN dotnet build CarInsuranceSalesBot.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish

ARG BUILD_CONFIGURATION=Release

# publish ready to use version
RUN dotnet publish CarInsuranceSalesBot.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#install runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0 as final

# Install fonts
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        fonts-dejavu-core \
        fonts-liberation \
        fontconfig && \
    fc-cache -f -v && \
    rm -rf /var/lib/apt/lists/*


# create appication folder
WORKDIR /app
# copy run files from build stage
COPY --from=publish /app/publish .

# run app
ENTRYPOINT ["dotnet", "CarInsuranceSalesBot.dll"]
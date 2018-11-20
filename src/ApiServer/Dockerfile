# ---
# First stage (build)
FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# Copy solution as distinct layer
COPY *.sln .
COPY ApiServer/*.csproj ./ApiServer/
RUN dotnet restore

# Copy everything else and build
COPY ApiServer/. ./ApiServer/
WORKDIR /app/ApiServer
RUN dotnet publish -c Release -o out

# ---
# Second stage (execution)
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/ApiServer/out ./

# Run on localhost:5004
ENV ASPNETCORE_URLS http://+:5004
EXPOSE 5004

# Drop privileges
USER 1000

ENTRYPOINT ["dotnet", "ApiServer.dll"]

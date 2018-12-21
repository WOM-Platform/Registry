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

# Run on localhost:8779
ENV ASPNETCORE_URLS http://+:8779
EXPOSE 8779

# Drop privileges
USER 1000

ENTRYPOINT ["dotnet", "ApiServer.dll"]

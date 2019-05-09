FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
ADD ./ ./

RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-runtime
WORKDIR /app
COPY --from=build-env /app/src/Microsoft.Azure.VirtualKubelet.Edge.Provider/out .
EXPOSE 5000
CMD ["/usr/bin/dotnet", "Microsoft.Azure.VirtualKubelet.Edge.Provider.dll"]
    
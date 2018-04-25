FROM microsoft/dotnet:2.0.0-runtime

WORKDIR /app

COPY ./ /app

EXPOSE 5000

CMD ["/usr/bin/dotnet", "Microsoft.Azure.VirtualKubelet.Edge.Provider.dll"]

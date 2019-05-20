FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS dotnet

COPY . /src/
RUN mkdir -p /home/site/wwwroot
RUN cd /src/CompressImagesFunction && dotnet publish CompressImagesFunction.csproj -c Release --output /home/site/wwwroot

# Native Binaries
RUN cd /src/CompressImagesFunction && cp bin/Release/netstandard2.0/bin/runtimes/linux-x64/native/libgit2-4aecb64.so /home/site/wwwroot/bin/
RUN cd /src/CompressImagesFunction && cp bin/Release/netstandard2.0/bin/runtimes/linux-x64/native/Magick.NET-Q16-x64.Native.dll.so /home/site/wwwroot/bin/

FROM mcr.microsoft.com/azure-functions/dotnet:2.0

RUN apt-get update && apt-get install -y --no-install-recommends --no-install-suggests \
  curl libcurl3

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
COPY --from=dotnet ["/home/site/wwwroot", "/home/site/wwwroot"]

FROM microsoft/dotnet:latest

LABEL description="Random Quotes API Docker image." \ 
      version="0.1" \
      author="cycprime"

# Version of this image
ENV QUOTEAPI_IMG_VER=0.1 \
    QUOTEAPI_IMG_NAME="quotesapi" \
    QUOTEAPI_IMG_DESC="Image for Random Quotes API."

# Copy the Quotes API DLL and other published directories and files 
# onto the /app directory.
COPY bin/Debug/netcoreapp1.0/publish/ /app/

# Copy any SSL certificates to the working directory.
COPY *.pfx /app/

# Create mount point for log directory for convenient backup.
VOLUME /app/Logs

# Create mount point for a data directory for convenient quotes addition.
VOLUME /app/Data

# Make the /app the working directory.
WORKDIR /app/

## RUN ["dotnet", "restore"]

## RUN ["dotnet", "build"]

# Image supports connection to ports 5000 and 5001 within the container.
EXPOSE 5000 5001

## ENTRYPOINT ["dotnet", "run", "--server.urls", "http://0.0.0.0:5000"]
## ENTRYPOINT dotnet /app/QuotesAPI.dll
ENTRYPOINT ["dotnet", "/app/QuotesAPI.dll"]

#!/bin/sh 
# We use above line becuase we deploy this in alpine linux where bin/bash is not present. 

set -e  # Exit immediately if a command fails

echo "Copying the podman systemd service file.."

if [ ! -f "/usr/share/hdns/hdns.service" ]; then
    cp /app/hdns/hdns.service /usr/share/hdns/hdns.service
fi

echo "Copying the logrotate file.."

if [ ! -f "/usr/share/hdns/hlog" ]; then
    cp /app/hdns/hlog /usr/share/hdns/hlog
fi

echo "Starting HDNS application..."

# Execute .NET application
exec dotnet /app/hdns/HDNS.dll
#!/bin/sh

echo "Writing config file..."
mkdir -p /etc/seaweedfs
printf '{
  "identities": [{
    "name": "fluxel",
    "credentials": [{
      "accessKey": "fluxel",
      "secretKey": "%s"
    }],
    "actions": [ "Admin", "Read", "Write", "List", "Tagging" ]
  }]
}' $AWS_SECRET_ACCESS_KEY > /etc/seaweedfs/s3.json

echo "Starting SeaweedFS"
weed server -s3 -s3.config=/etc/seaweedfs/s3.json -dir=/data -master.telemetry=false
#!/bin/sh
set -e
: "${API_URL:=http://localhost:8080}"
envsubst '${API_URL}' \
  < /usr/share/nginx/html/assets/config.json.template \
  > /usr/share/nginx/html/assets/config.json

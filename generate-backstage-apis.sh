#!/bin/bash

for f in `ls swagger/`
do
	theVersion=`echo $f|gawk 'match($0, "swagger\.(.*).yaml", m) {print m[1]}' 2>/dev/null`
	cat microservice-api.yaml|sed "s/%##API_VER##%/$theVersion/" > microservice-api-$theVersion.yaml

	theServiceAPI=`cat microservice-api.yaml|gawk 'match($0, "name: (.*)-.*-api", m) {print m[1]}' 2>/dev/null`

	echo "    - $theServiceAPI-$theVersion-api" >> catalog-info.yaml
done
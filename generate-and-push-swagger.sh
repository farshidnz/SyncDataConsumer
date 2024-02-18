#!/bin/bash

for d in `ls -d src/WebUI/Controllers/*/`
do
    theVersion=`echo $d | gawk 'match($0, ".*\/(.*)\/", m) {print m[1]}' 2>/dev/null`
    dotnet swagger tofile --yaml --output ./swagger/swagger.$theVersion.yaml src/WebUI/bin/Release/net5.0/accountsSyncDataConsumer.API.dll $theVersion
done

# we only want to push if there are updates to the swagger
if [ -n "`git status -s`" ]
then
	git add ./swagger/*
	git commit -m "[skip ci] Swagger updates from pipeline"
	git push origin ${BITBUCKET_BRANCH}
fi
#!/usr/bin/env node
import { getEnv, getResourceName } from "@cashrewards/cdk-lib";
import { App } from "aws-cdk-lib";
import "source-map-support/register";
import { DeployStack } from "./lib/deploy-stack";

const app = new App();
new DeployStack(app, getResourceName("accountsSyncDataConsumer"), {
  env: {
    account: getEnv("AWS_ACCOUNT_ID"),
    region: getEnv("AWS_REGION"),
  },
});

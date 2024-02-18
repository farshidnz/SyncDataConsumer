import { App } from "aws-cdk-lib";
import { Match, Template } from "aws-cdk-lib/assertions";
import { DeployStack } from "../lib/deploy-stack";
import { StartsWithMatch } from "./helper";

describe("Should create CDK deployment stack ", () => {
  process.env.AWS_ACCOUNT_ID = "1234567890";
  process.env.AWS_REGION = "ap-southeast-2";
  process.env.ENVIRONMENT_NAME = "test";
  process.env.PROJECT_NAME = "dotnet5tempalte";
  process.env.VERSION = "latest";
  process.env.ScalingMinCapacity = '2';
  process.env.ScalingMaxCapacity = '5';
  process.env.ScalingDesiredCount = '3';
  process.env.cpu = '512';
  process.env.memoryLimitMiB = '1024';
  process.env.Environment = "test";
  process.env.MemberTopicArn = "arn:aws:sns:ap-southeast-2:752830773963:Test";
  process.env.ShopGoDBPassword = "/syncdataconsumer/default/ShopGoDBPassword";
  process.env.AzureAADClientId = "/syncdataconsumer/default/AzureAADClientId";
  process.env.AzureAADClientSecret = "/syncdataconsumer/default/AzureAADClientSecret";

  const app = new App();
  // WHEN
  const stack = new DeployStack(app, "testStack", {
    env: {
      account: process.env.AWS_ACCOUNT_ID,
      region: process.env.AWS_REGION,
    },
  });
  const template = Template.fromStack(stack);

  test("Should ecs fargate service", () => {
    template.hasResourceProperties("AWS::ECS::Service", {
      Cluster: `${process.env.ENVIRONMENT_NAME}-ecs`,
      DeploymentConfiguration: {
        MaximumPercent: 200,
        MinimumHealthyPercent: 50,
      },
      DesiredCount: 1,
      EnableECSManagedTags: false,
      EnableExecuteCommand: true,
      HealthCheckGracePeriodSeconds: 60,
      LaunchType: "FARGATE",
      LoadBalancers: [
        {
          ContainerName: `${process.env.ENVIRONMENT_NAME}-dotnet5tempalte-container`,
          ContainerPort: 80,
          TargetGroupArn: {
            Ref: new StartsWithMatch(
              "TargetGroupArn",
              `${process.env.ENVIRONMENT_NAME}ecs${process.env.ENVIRONMENT_NAME}dotnet5tempaltetg`
            ),
          },
        },
      ],
      NetworkConfiguration: {
        AwsvpcConfiguration: {
          AssignPublicIp: "DISABLED",
          SecurityGroups: [
            {
              "Fn::GetAtt": [
                new StartsWithMatch(
                  "SecurityGroups",
                  `${process.env.ENVIRONMENT_NAME}ecs${process.env.ENVIRONMENT_NAME}dotnet5tempaltesg`
                ),
                "GroupId",
              ],
            },
          ],
          Subnets: [
            new StartsWithMatch("Subnet1", "p-"),
            new StartsWithMatch("Subnet2", "p-"),
          ],
        },
      },
      ServiceName: `${process.env.ENVIRONMENT_NAME}-dotnet5tempalte-ecsService`,
      TaskDefinition: {
        Ref: new StartsWithMatch(
          "TaskDefinition",
          `${process.env.ENVIRONMENT_NAME}ecs${process.env.ENVIRONMENT_NAME}dotnet5tempalteecsTD`
        ),
      },
      Tags: [
        {   
          Key: "Application",
          Value: "accountsSyncDataConsumer"
        },
        {
          Key: "Team",
          Value: "Accounts"
        }
      ]
    });
  });
});

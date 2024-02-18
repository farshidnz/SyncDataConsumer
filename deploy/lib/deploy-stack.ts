import { DomainEventFilter, DomainTopicSubscriptionConstruct, EcsConstruct, getEnv, getResourceName, ServiceVisibility, applyMetaTags } from "@cashrewards/cdk-lib";
import { Duration, Stack, StackProps, Tags } from "aws-cdk-lib";
import { PolicyStatement } from "aws-cdk-lib/aws-iam";
import { Topic } from "aws-cdk-lib/aws-sns";
import { Construct } from "constructs";

export class DeployStack extends Stack {
  protected ecsConstruct: EcsConstruct;
  constructor(scope: Construct, id: string, props?: StackProps) {
    super(scope, id, props);

    this.ecsConstruct = new EcsConstruct(this, getResourceName("ecs"), {
      environmentName: getEnv("ENVIRONMENT_NAME"),
      serviceName: getEnv("PROJECT_NAME"),
      customDomain: "accountssyncdata",
      healthCheckPath: "health-check",
      visibility: ServiceVisibility.PRIVATE,
      listenerRulePriority: 3900,
      imageTag: getEnv("VERSION"),
      minCapacity: +getEnv('ScalingMinCapacity'),
      maxCapacity: +getEnv('ScalingMaxCapacity'),
      desiredCount: +getEnv('ScalingDesiredCount'),
      cpu: +getEnv('cpu'),
      memoryLimitMiB: +getEnv('memoryLimitMiB'),
      scalingRule: {
        cpuScaling: {
          targetUtilizationPercent: 60,
          scaleInCooldown: 60,
          scaleOutCooldown: 60,
          alarm: {
            enableSlackAlert: true,
          },
        },
        memoryScaling: {
          targetUtilizationPercent: 70,
          scaleInCooldown: 60,
          scaleOutCooldown: 60,
          alarm: {
            enableSlackAlert: true,
            threshold: 60,
          },
        },
      },
      environment: {
        Environment: getEnv("Environment"),
        ServiceName: getEnv("PROJECT_NAME"),
        LOG_LEVEL: getEnv("LOG_LEVEL"),
        SQLServerHostWriter: getEnv("SQLServerHostWriter"),
        ShopGoDBName: getEnv("ShopGoDBName"),
        ShopGoDBUser: getEnv("ShopGoDBUser"),
      },
      secrets: {
        ShopGoDBPassword: getEnv("ShopGoDBPassword"),
        AzureAADClientId: getEnv("AzureAADClientId"),
        AzureAADClientSecret: getEnv("AzureAADClientSecret")
      },
      useOpenTelemetry: true
    });
    
    const topic = Topic.fromTopicArn(
      this,
      "MemberTopicArn",
      getEnv("MemberTopicArn")
    );
    topic.grantPublish(this.ecsConstruct.taskRole);
    this.ecsConstruct.taskRole.addToPrincipalPolicy(new PolicyStatement({
       actions: [ 
	      "SNS:ListTopics",
        "kms:Decrypt"
		],
       resources: [ "*" ]
    }));

    let domainTopicSub = new DomainTopicSubscriptionConstruct(this, getResourceName("MemberTopicSubscription"), {
      domain: "Member",
      environmentName: getEnv("ENVIRONMENT_NAME"),
      serviceName: getEnv("PROJECT_NAME"),
      maximumMessageCount: 10,
      filterPolicy: DomainEventFilter.ByEventType(["MemberJoined", "MemberDetailChanged", "CognitoLinked"]) // Filter for the events you're interested in, if blank, you'll get everything
    });
    
    domainTopicSub.messageQueue.grantConsumeMessages(this.ecsConstruct.taskRole);
    domainTopicSub.messageQueue.grantPurge(this.ecsConstruct.taskRole);

    this.ecsConstruct.node.addDependency(domainTopicSub);

    applyMetaTags(this, {'Team': 'Accounts', 'Application': 'accountsSyncDataConsumer'});

    
  }

  get ecsConstructObj() {
    return this.ecsConstruct;
  }

  
}


# Cashrewards microservice template for .net 6.0

This is a .net template for backend micro-services (as of writing this document updated to .net 6).

## Creating new microservice using Backstage (Recommended)

If you want to create your own microservice repo based on this template <br> please use Backstage to do so by following this guidline
https://shopgoau.atlassian.net/wiki/spaces/DEV/pages/2493743207/DevOps+4.0+-+Backstage.

## Manually creating new microservice

import this template
1) Clone this repo, from the repo root execute cmd...
2) **dotnet new --install .**

Create a new service using the template
1) Create new folder same as new project name, then inside that folder execute cmd...
2) **dotnet new cr-micro-net5**
<br>it'll use the current folder name as the project name

# Description

This sample template project for .net microservices is based on the CLEAN architecture. <br>
CLEAN architecture follows dependency inversion principle and you can think of the project structure as a 3 layer onion.

* Outermost layer consists of API/presentation and Infrastructure/persistence.
* Next is the Application layer with application specific business logic and has no dependency on outer layer.
* Innermost is the Domain with no dependencies on any outer layers.

Since the inner core has no dependencies on outer presentation/persistence this naturally lends itself to Domain Driven Design. <br>
There is no need to pollute your domain model with database and UI concerns. <br>
You can see in the example code that the project combines CQRS and MassTransit MediatR to route messages within the service. <br> 
There is a segregation of commands and queries within the application layer. <br>
Mediator makes it easy to send these requests and get responses from application layer, <br>
as well as providing the ability to attach filters before and after the requests to the application layer (see ValidationBehaviour.cs for example).

## API

You can see in the example code there are 2 versions of the PersonController, with 2 versions of the GET PersonInfo viewmodel. <br>
To accommodate this there are "v1" and "v2" folders within the GetPersonInfo use case folder of application layer. <br>
So the controllers simply use the corresponding namespace of the application layer when issuing the right version of commands/queries. <br>
When you run the API, swagger will load up and allow you to switch between versions of API.

There is authentication built in to authenticate Cognito bearer tokens. You can enable authentication via [Authorize] attribute on controllers.

## Persistence

In this template an In-memory persistence context is used in the infrastructure. Since the application layer or domain does not depend on <br>
concrete implementation of the persistence context, you can easily swap out the in-memory implementation(in the infrastructure layer) <br>
to your choice of persistence technology, whether it be a relational database or a document database. Since persistence is an infrastructure  <br>
concern, it should not affect the design of your domain model.

## Domain Events

This template has an implementation of the event oriented architecture standard defined in https://shopgoau.atlassian.net/wiki/spaces/DEV/pages/2548236326/Event+Oriented+Architecture+EOA+-+WIP. <br>
Both publishing and reading of events are delegated to the infrastruture layer such that developers can focus on the application layer to easily raise outgoing events and handle
incomming events.

### Publishing Events

In order to publish events in the template, first you must add EventDestinations section in the appsettings.json file. <br>
which defines the destination SNS topic your events will be published to. Currently only SNS is supported when publishing events.
```
"EventDestinations": {
    "AWSResources": [
      {
        "Type": "SNS",
        "Domain": "Member",
        "EventTypeName": "PersonEmoted"
      },
      {
        "Type": "SNS",
        "Domain": "Member",
        "EventTypeName": "PersonBorn"
      }
    ]
  }
```

Then it is a simple matter of Raising events within the application layer or domain.
```
public async Task Consume(ConsumeContext<CreatePersonCommand> context)
{
    var personId = new PersonId(context.Message.PersonId);

    var person = new Person(personId);
    person.RaiseEvent(new PersonBorn
    {
        PersonID = personId.ToString(),
        DateOfBirth = DateTime.Now
    });

    await persistenceContext.Save(person);
}
```

Note raising events is not the same as publishing, RaiseEvent will add your domain event to the domain entity such that it will be published at a later time. <br>
This is because of the outbox pattern. Idea is any domain events getting raised would get persisted into an outbox within the
same transaction scope thats persisting the domain entity.  <br>
This decouples the raising of events with the publication of the events and helps avoid inconsistencies that can arise <br>
if event publication fails for some reason (since the state of the domain entity is now in sync with the state of the outbox ). <br>
Depending on your choice of persistance(relational/document db) you need to implement the appropriate transaction scope when saving the domain entity. <br>
The actual publication of events from outbox is triggered in two ways, first after persisting any change to domain entity <br> 
and secondly via a background service monitoring/publishing any pending events in the outbox.

### Subscribing to Events
 
 When reading events you must add EventSources section in the appsettings.json file. <br>
 Currently only reading from SQS queues is supported along with 2 types of reading modes. <br>
 For services that are planned to be deployed in ECS, there is a PolledRead mode that will poll for events periodically. <br>
 For services that are planned to be deployed as lambda, there is a LambdaTrigger mode that will read events from the trigger function. <br>
 
##### Polled reading mode

For polled reading, add to the appsettings and ensure the read mode is set to polled reads.
```
"EventSources": {
    "AWSResources": [
      {
        "Type": "SQS",
        "Domain": "Member",
        "EventTypeName": "PersonEmoted",
        "ReadMode": "PolledRead"
      },
      {
        "Type": "SQS",
        "Domain": "Member",
        "EventTypeName": "PersonBorn",
        "ReadMode": "PolledRead"
      }
    ]
  },
```
Then it is a simple matter of adding an event handler to your application layer.
```
public class PersonBornEventHandler : IConsumer<DomainEventNotification<PersonBorn>>
{
    public async Task Consume(ConsumeContext<DomainEventNotification<PersonBorn>> context)
    {
        var pesonBornEvent = context.Message.DomainEvent;
    }
}
```
Next time a PersonBorn event is received by the SQS, it will be read from the SQS and passed to the consumer function. <br>
If there are no exceptions when consuming the event, it will be removed from the SQS as it is considered to be successfully processed. <br>
However if there is an exception thrown, then the event wont be removed from SQS, and during the next poll reading cycle it is read <br>
again for processing. The default poll reading cycle is every 3 minutes and can be changed in the source code. <br>

##### Lambda triggered reading mode

For lambda triggers, add to the appsettings and ensure the read mode is set to lambda trigger.
```
"EventSources": {
    "AWSResources": [
      {
        "Type": "SQS",
        "Domain": "Member",
        "EventTypeName": "PersonEmoted",
        "ReadMode": "LambdaTrigger"
      },
      {
        "Type": "SQS",
        "Domain": "Member",
        "EventTypeName": "PersonBorn",
        "ReadMode": "LambdaTrigger"
      }
    ]
  },
```
You will need to configure your SQS such that it triggers your lambda and call the function handler
```
"function-handler": "accountsSyncDataConsumer.Infrastructure::accountsSyncDataConsumer.Infrastructure.AWS.LambdaEventTriggerHandler::ReadEvents"
```
Then it is a simple matter of adding an event handler to your application layer.
```
public class PersonBornEventHandler : IConsumer<DomainEventNotification<PersonBorn>>
{
    public async Task Consume(ConsumeContext<DomainEventNotification<PersonBorn>> context)
    {
        var pesonBornEvent = context.Message.DomainEvent;
    }
}
```
Next time a PersonBorn event is received by the SQS, it will trigger the function handler and the function handler will route the event to your event handler. <br>
If there are no exceptions when consuming the event, it will be removed from the SQS as it is considered to be successfully processed. <br>
However if there is an exception thrown, then the event wont be removed from SQS and it will retry triggering your lambda again for reprocessing. <br>

There is a built in Lambda test tool which you can launch using "Mock Lambda Test Tool" option from the visual studio launcher dropdown. <br>
Make sure you have got the tools installed by running the command
````
dotnet tool install -g Amazon.Lambda.TestTool-6.0
````
This test tool can be used to simulate triggering of the function handler when debugging locally.


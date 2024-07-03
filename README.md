# mass-transit-trivia
Code sample build to support a December 2023 conference talk: "Event Orchestration with State Machines"

# Trivia Game
A trivia game has been chosen as a non-trivial use case to present async event orchestration.

# build
```
dotnet build
```

# tests
Tests include integration test using TestContaners for Dotnet. 
A current working docker implementation is required for these.

```
dotnet test
```


# aws deployment
Login to AWS cli:
```
aws sso login --profile <aws-profile>
```

deploy `UserStack` first - gives us a user we can use to deploy sqs/sns topology
```
cdk deploy "brendan-trivia-user-stack" --profile <aws-profile> 
```

setup keys via dotnet secrets and deploy SNS/SQS topology
```
cd ./MttDeployTopology
dotnet run
```
bug: run this many times to get all topic->queue permissions deployed

deploy main stack
```
cdk deploy "brendan-trivia-stack" --profile <aws-profile> 
```



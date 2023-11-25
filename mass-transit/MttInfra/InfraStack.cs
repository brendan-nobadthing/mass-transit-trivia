using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Constructs;
using InstanceType = Amazon.CDK.AWS.RDS.InstanceType;

namespace MttInfra;

public class InfraStack: Stack
{
    internal InfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        
        
        var apiLambda = new Function(this, "MttApiLambda", new FunctionProps
        {
            Runtime = Runtime.DOTNET_6,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = "MttApi",
            Code = Code.FromAsset("../", new Amazon.CDK.AWS.S3.Assets.AssetOptions // parent path to include MttApi's dependencies. Use with workingdirectory bundling value
            {
                Bundling = new BundlingOptions()
                {
                    Image = Runtime.DOTNET_6.BundlingImage,
                    User = "root",
                    OutputType = BundlingOutput.ARCHIVED,
                    WorkingDirectory = "/asset-input/MttApi/src/MttApi",
                    Command = new string[]
                    {
                        "/bin/sh",
                        "-c",
                        " dotnet tool install -g Amazon.Lambda.Tools" +
                        " && dotnet build" +
                        " && dotnet lambda package --output-package /asset-output/function.zip"
                    }
                }
            }),
        });

        // publish to sns topics prefixed with brendan-trivia 
        apiLambda.Role?.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps()
        {
            Effect = Effect.ALLOW,
            Actions = new []{ "sns:Publish" },
            Resources = new []{ $"arn:aws:sns:{this.Region}:{this.Account}:brendan-trivia*" }
        }));
        // list all topics under account
        apiLambda.Role?.AddToPrincipalPolicy(new PolicyStatement(new PolicyStatementProps()
        {
            Effect = Effect.ALLOW,
            Actions = new []{ "sns:ListTopics" },
            Resources = new []{ $"arn:aws:sns:{this.Region}:{this.Account}:*" }
        }));
        
        //Proxy all request from the root path "/" to Lambda Function One
        var restAPI = new LambdaRestApi(this, "Endpoint", new LambdaRestApiProps
        {
            Handler = apiLambda,
            Proxy = true,
        });
        
        new CfnOutput(this, "apigwtarn", new CfnOutputProps { Value = restAPI.ArnForExecuteApi() });
        
        
        // Database for saga persistence
        var dbVpc = new Vpc(this, "vpc-postgres", new VpcProps
        {
            Cidr = "10.0.0.0/16",
            MaxAzs = 2,
            SubnetConfiguration = new ISubnetConfiguration[]
            {
                new SubnetConfiguration
                {
                     CidrMask = 24,
                     SubnetType = SubnetType.PUBLIC,
                     Name = "MyPublicSubnet"
                },
                new SubnetConfiguration
                {
                    CidrMask = 24,
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                    Name = "MyPrivateSubnet"
                }
            }
        });
        var db = new DatabaseInstance(this, "DB", new DatabaseInstanceProps
        {
            Vpc = dbVpc,
            VpcSubnets = new SubnetSelection{ SubnetType = SubnetType.PRIVATE_WITH_EGRESS },
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps(){ Version = PostgresEngineVersion.VER_15 }),
            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.T3, InstanceSize.MICRO),
            Port = 5432,
            InstanceIdentifier = "brendan-trivia-db",
            BackupRetention = Duration.Seconds(0) //not a good idea in prod, for this sample code it's ok
        });

        var dbSecurityGroup = new SecurityGroup(this, "db-security-group", new SecurityGroupProps()
        {
            Vpc = dbVpc,
        });
        dbSecurityGroup.Connections.AllowFrom(Peer.AnyIpv4(), Port.Tcp(5432), "Postgres");

        var gameStateQueue =
            Queue.FromQueueArn(this, "game-state", $"arn:aws:sqs:{this.Region}:{this.Account}:game-state");
        

        var backendLambda = new Function(this, "MttBackendLambda", new FunctionProps
        {
            Runtime = Runtime.DOTNET_6,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = "MttBackend::MttBackend.Function::SQSHandler",
            Vpc = dbVpc,
            Code = Code.FromAsset("../", new Amazon.CDK.AWS.S3.Assets.AssetOptions // parent path to include MttApi's dependencies. Use with workingdirectory bundling value
            {
                Bundling = new BundlingOptions()
                {
                    Image = Runtime.DOTNET_6.BundlingImage,
                    User = "root",
                    OutputType = BundlingOutput.ARCHIVED,
                    WorkingDirectory = "/asset-input/MttBackend/src/MttBackend",
                    Command = new string[]
                    {
                        "/bin/sh",
                        "-c",
                        " dotnet tool install -g Amazon.Lambda.Tools" +
                        " && dotnet build" +
                        " && dotnet lambda package --output-package /asset-output/function.zip"
                    }
                }
            }),
        });
        backendLambda.AddEventSource(new SqsEventSource(gameStateQueue));
        // var backendApi = restAPI.Root.AddResource("backend", new ResourceOptions()
        // {
        //     DefaultIntegration = new LambdaIntegration(backendLambda)
        // });
        // backendApi.AddMethod("ANY");
        // backendApi.AddProxy();



    }
}
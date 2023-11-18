using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace MttInfra;

public class InfraStack: Stack
{
    internal InfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        var buildOption = new BundlingOptions()
        {
            Image = Runtime.DOTNET_6.BundlingImage,
            User = "root",
            OutputType = BundlingOutput.ARCHIVED,
            Command = new string[]
            {
                "/bin/sh",
                "-c",
                " dotnet tool install -g Amazon.Lambda.Tools" +
                " && dotnet build" +
                " && dotnet lambda package --output-package /asset-output/function.zip"
            }
        };
        
        var lambda1 = new Function(this, "lambda1", new FunctionProps
        {
            Runtime = Runtime.DOTNET_6,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = "MttLambda::MttLambda.Functions_Get_Generated::Get",
            Code = Code.FromAsset("../MttLambda/src/MttLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = buildOption
            }),
        });
        
        
        //Proxy all request from the root path "/" to Lambda Function One
        var restAPI = new LambdaRestApi(this, "Endpoint", new LambdaRestApiProps
        {
            Handler = lambda1,
            Proxy = true,
        });
        
        new CfnOutput(this, "apigwtarn", new CfnOutputProps { Value = restAPI.ArnForExecuteApi() });

    }
}
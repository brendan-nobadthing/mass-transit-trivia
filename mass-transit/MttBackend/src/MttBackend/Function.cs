using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MttBackend;

public class Function
{

    public Function()
    {
        
    }
    
    public async Task SQSHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
           Console.Out.WriteLine("SQS Handler Got Message: "+message.Body);
        }
    }

}
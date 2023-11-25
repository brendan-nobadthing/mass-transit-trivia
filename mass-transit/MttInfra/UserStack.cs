using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace MttInfra;

/// <summary>
/// Bootstrap a user account that will allow mass transit to deploy messaging topology
/// </summary>
public class UserStack: Stack
{
    internal UserStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        
        var userName = "brendan-trivia-user";
        var user = new User(this, "brendan-trivia-user", new UserProps()
        {
            UserName = userName
        });
        var accessKey = new AccessKey(this, "brendan-trivia-user-key", new AccessKeyProps()
        {
            User = user
        });
        
        user.AddToPolicy(new PolicyStatement(new PolicyStatementProps()
        {
            Effect = Effect.ALLOW,
            Actions = new[] { "sqs:*", "sns:*" },
            Resources = new[] { $"arn:aws:sqs:{this.Region}:{this.Account}",  $"arn:aws:sns:{this.Region}:{this.Account}"}
        }));
        
        new CfnOutput(this, "brendan-trivia-user-access-key", new CfnOutputProps() { Value = accessKey.AccessKeyId});
        new CfnOutput(this, "brendan-trivia-user-secret", new CfnOutputProps() { Value = accessKey.SecretAccessKey.UnsafeUnwrap() });

        var applicationSecrets = new Secret(this, "application-secrets", new SecretProps()
        {
            SecretName = "application-secrets",
            SecretObjectValue = new Dictionary<string, SecretValue>()
            {
                { "user-access-key", SecretValue.ResourceAttribute(accessKey.AccessKeyId) },
                { "user-secret", accessKey.SecretAccessKey }
            }
        });
        
    }
}
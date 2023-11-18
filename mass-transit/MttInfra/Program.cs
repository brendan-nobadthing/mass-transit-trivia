using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MttInfra;

sealed class Program
{
    public static void Main(string[] args)
    {
        var app = new App();
        new InfraStack(app, "brendan-trivia-stack", new StackProps
        {
            // If you don't specify 'env', this stack will be environment-agnostic.
            // Account/Region-dependent features and context lookups will not work,
            // but a single synthesized template can be deployed anywhere.

            // Uncomment the next block to specialize this stack for the AWS Account
            // and Region that are implied by the current CLI configuration.
            /*
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
            }
            */

            // Uncomment the next block if you know exactly what Account and Region you
            // want to deploy the stack to.
            /*
           
            */

            // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
            Env = new Amazon.CDK.Environment
            {
                Account = "864141050364",
                Region = "ap-southeast-2",
            }

            
        });
        app.Synth();
    }
}

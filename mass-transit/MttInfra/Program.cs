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
        

        new UserStack(app, "brendan-trivia-user-stack", new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = "864141050364",
                Region = "ap-southeast-2",
            }
        });

        new InfraStack(app, "brendan-trivia-stack", new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = "864141050364",
                Region = "ap-southeast-2",
            }
        });
        app.Synth();
    }
}

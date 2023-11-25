using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SQS;
using Shouldly;

namespace MttSqsTests;

public class SqsTests
{


    [Fact]
    public async Task QueueShouldExist()
    {
        //var ssoCreds = LoadSsoCredentials("digio-sandbox");
        var ssoCreds = new BasicAWSCredentials("AKIA4SMWSAH6KVF7EFXY", 
            "6GzH/h/vAr8rxv3fbj02AwmGYrN4e6tsRtfVTns1");
        var ssoProfileClient = new AmazonSecurityTokenServiceClient(ssoCreds);

        var sqsClient = new AmazonSQSClient(ssoCreds, RegionEndpoint.APSoutheast2);

        var listQueuesResponse = await sqsClient.ListQueuesAsync("brendan-trivia-stack");
        listQueuesResponse.QueueUrls.ShouldNotBeEmpty();
    }
    
    
    
    //
    // Method to get SSO credentials from the information in the shared config file.
    static AWSCredentials LoadSsoCredentials(string profile)
    {
        var chain = new CredentialProfileStoreChain();
        if (!chain.TryGetAWSCredentials(profile, out var credentials))
            throw new Exception($"Failed to find the {profile} profile");
        return credentials;
    }
    
}
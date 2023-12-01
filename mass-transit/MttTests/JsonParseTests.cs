using System.Text.Json.Nodes;
using Shouldly;

namespace MttTests;

public class JsonParseTests
{
    
    [Fact]
    private void ShouldHandleNumbersAsStrings() // want this behaviour when fetching aws secrets
    {
        var json = @"
{
    ""stringVal"": ""testString"",
    ""numVal"":12
}
";
        
        var jsonObj = JsonNode.Parse(json)?.AsObject();
        var stringVal =  jsonObj?["stringVal"]?.AsValue().ToString();
        var numVal =  jsonObj?["numVal"]?.AsValue().ToString();
        
        stringVal.ShouldBe("testString");
        numVal.ShouldBe("12");
        numVal.GetType().ShouldBe(typeof(string));
    }
}
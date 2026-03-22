using EVotingSystem.Tests.Controllers;
using EVotingSystem.Tests.Infrastructure;
using EVotingSystem.Tests.Services;
using EVotingSystem.Tests.Testing;

var testCases = new List<TestCase>();
testCases.AddRange(MailcheckEmailValidationServiceTests.GetCases());
testCases.AddRange(AccountControllerTests.GetCases());
testCases.AddRange(VotingServiceTests.GetCases());
testCases.AddRange(ResultsDashboardCalculatorTests.GetCases());
testCases.AddRange(FirestoreVotingTransactionTests.GetCases());

var failed = 0;
foreach (var testCase in testCases)
{
    try
    {
        await testCase.RunAsync();
        Console.WriteLine($"PASS {testCase.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.WriteLine($"FAIL {testCase.Name}");
        Console.WriteLine(exception);
    }
}

Console.WriteLine();
Console.WriteLine($"Executed {testCases.Count} tests. Failures: {failed}.");

return failed == 0 ? 0 : 1;

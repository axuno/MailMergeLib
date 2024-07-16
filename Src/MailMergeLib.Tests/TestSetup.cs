using System;
using System.IO;
using NUnit.Framework;

namespace MailMergeLib.Tests;

[SetUpFixture]
public class TestSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Disable console output from test methods
        Console.SetOut(TextWriter.Null);
    }

    [OneTimeTearDown]
    public void RunAfterAnyTests()
    {
        // Nothing defined here
    }
}

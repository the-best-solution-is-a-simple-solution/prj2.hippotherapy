using HippoApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace tests;

/// <summary>
///     A template for making more unit test files
/// </summary>
public class TestFileTemplate : IDisposable
{
    private readonly TestServer _testServer;


    /// <summary>
    ///     Create _testServer an "In memory" server that you can make requests to without having to set the base address.
    /// </summary>
    public TestFileTemplate()
    {
        _testServer = new TestServer(new WebHostBuilder()
            .ConfigureAppConfiguration((context, builder) => { builder.AddJsonFile("appsettings.Emulator.json"); })
            .UseStartup<StartupEmulator>());
    }

    public void Dispose()
    {
        _testServer.Dispose();
    }


    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}
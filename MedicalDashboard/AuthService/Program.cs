using AuthService;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config
            .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
            .AddEnvironmentVariables();

        if (hostingContext.HostingEnvironment.EnvironmentName == "Development")
        {
            config.AddJsonFile("appsettings.Local.json", true, true);
        }
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
            .UseStartup<Startup>(); 
    });

builder.Build().Run();
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Irudd.Api;

public class ApiApplication
{
    private readonly WebApplication app;
    
    private ApiApplication(WebApplication app)
    {
        this.app = app;
    }

    public static ApiApplication Create(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container.

        builder.Services.AddControllers();
        
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.AddSwaggerGen(options =>
        {
            var xmlDocumentationFilename = Path.Combine(
                AppContext.BaseDirectory, 
                $"{Assembly.GetEntryAssembly().Require("Entry assembly missing").GetName().Name}.xml");
            if (!File.Exists(xmlDocumentationFilename))
            {
                throw new Exception(@"The xml documentation file needed for swagger docs to work is missing. You most likely forgot to add the following to your api csproj file:
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>");
            }
            options.IncludeXmlComments(xmlDocumentationFilename);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        
        return new ApiApplication(app);
    }

    public void Run()
    {
        app.Run();
    }
}
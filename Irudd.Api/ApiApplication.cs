using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.MiddlewareAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DiagnosticAdapter;
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
        
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        // Add services to the container.
        /*
         * We do the below instead of just: builder.Services.AddMiddlewareAnalysis(); 
         * to force this filter to be first in the pipeline.
         * If we dont do this we cant see things happening from the services registered by CreateBuilder
         */
        builder.Services.Insert(0, ServiceDescriptor.Transient<IStartupFilter, AnalysisStartupFilter>());
        
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
/*
        var listener = app.Services.GetRequiredService<DiagnosticListener>();

        // Create an instance of the AnalysisDiagnosticAdapter using the IServiceProvider
        // so that the ILogger is injected from DI
        var observer = ActivatorUtilities.CreateInstance<AnalysisDiagnosticAdapter>(app.Services);

        // Subscribe to the listener with the SubscribeWithAdapter() extension method
        using var disposable = listener.SubscribeWithAdapter(observer);                   
  */
        var app = builder.Build();
        var logger = app.Services.GetService<ILogger<AnalysisDiagnosticAdapter>>();
        DiagnosticListener.AllListeners.Subscribe(new MiddlewareAnalysisDiagnosticListenerObserver(logger));

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

public class MiddlewareAnalysisDiagnosticListenerObserver : IObserver<DiagnosticListener>
{
    private readonly ILogger<AnalysisDiagnosticAdapter> logger;

    public MiddlewareAnalysisDiagnosticListenerObserver(ILogger<AnalysisDiagnosticAdapter> logger)
    {
        this.logger = logger;
    }
    public void OnCompleted()
    {
        
    }

    public void OnError(Exception error)
    {
        
    }

    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == "Microsoft.Extensions.Hosting" || value.Name == "Microsoft.AspNetCore")
        {
            //value.Subscribe(new MiddlewareAnalysisDiagnosticEventObserver());
            //value.SubscribeWithAdapter(new ValueListenerAdapter());
            value.SubscribeWithAdapter(new AnalysisDiagnosticAdapter(logger));
        }
    }
}

public class MiddlewareAnalysisDiagnosticEventObserver : IObserver<KeyValuePair<string, object?>>
{
    public MiddlewareAnalysisDiagnosticEventObserver()
    {

    }
    
    public void OnCompleted()
    {
        
    }

    public void OnError(Exception error)
    {
        
    }

    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key == "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")
        {
            // HttpContext httpContext, string name, Guid instance, long timestamp
            Console.WriteLine(value.Value);    
        }
    }
}

public class ValueListenerAdapter
{
    // 👇 The [DiagnosticName] attribute describes which events to listen to
    [DiagnosticName("Microsoft.AspNetCore.Hosting.BeginRequest")]
    public virtual void OnBeginRequest(HttpContext httpContext, long timestamp)
    {
        // 👆 the signature of the method is used to extract information from
        // the anonymous type. You still need to know the names of the properties 
        // and their types, but no reflection in user-code is required
        Console.WriteLine($"Request started at {timestamp} to {httpContext.Request.Path}");
    }
}
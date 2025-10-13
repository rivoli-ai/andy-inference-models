using DebertaInferenceModel.Api.Configuration;
using DebertaInferenceModel.Api.Models;
using DebertaInferenceModel.Api.Services;
using DebertaInferenceModel.ML.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "DeBERTa Prompt Guard API", 
        Version = "v1",
        Description = "API for detecting prompt injection attacks using DeBERTa v3 model"
    });
});

// Configure model settings
builder.Services.Configure<ModelConfiguration>(
    builder.Configuration.GetSection(ModelConfiguration.SectionName));

// Register PromptGuardServiceWrapper as singleton (with fallback support)
builder.Services.AddSingleton<PromptGuardServiceWrapper>(sp =>
{
    var config = builder.Configuration.GetSection(ModelConfiguration.SectionName).Get<ModelConfiguration>();
    
    if (config == null)
    {
        throw new InvalidOperationException("ModelConfiguration is not properly configured");
    }

    // Use TokenizerServiceUrl if provided, otherwise fall back to TokenizerPath
    var tokenizerPathOrUrl = !string.IsNullOrWhiteSpace(config.TokenizerServiceUrl)
        ? config.TokenizerServiceUrl
        : config.TokenizerPath;

    return new PromptGuardServiceWrapper(config.ModelPath, tokenizerPathOrUrl);
});

// Register PredictionLoggerService as singleton
builder.Services.AddSingleton<PredictionLoggerService>(sp =>
{
    var maxLogSize = builder.Configuration.GetValue<int>("Logging:MaxLogSize", 1000);
    return new PredictionLoggerService(maxLogSize);
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DeBERTa Prompt Guard API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

// Health check endpoint
app.MapGet("/health", ([FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    try
    {
        // Try a simple prediction to verify service is working
        var testResult = promptGuard.Predict("test");
        
        if (promptGuard.IsUsingFallback)
        {
            return Results.Json(
                new 
                { 
                    status = "degraded",
                    modelLoaded = false,
                    modelReady = false,
                    usingFallback = true,
                    timestamp = DateTime.UtcNow,
                    message = "Model failed to load. Using fallback keyword detection.",
                    warning = promptGuard.ModelError
                },
                statusCode: 200); // Still return 200 since API is functional
        }
        
        return Results.Ok(new 
        { 
            status = "healthy",
            modelLoaded = true,
            modelReady = true,
            usingFallback = false,
            timestamp = DateTime.UtcNow,
            message = "Model is loaded and ready for predictions"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new 
            { 
                status = "unhealthy",
                modelLoaded = false,
                modelReady = false,
                usingFallback = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message
            },
            statusCode: 503);
    }
})
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi()
.WithSummary("Check API and model health")
.WithDescription("Verifies that the API is running and the ML model is loaded and ready");

// Detailed health check endpoint
app.MapGet("/health/detailed", ([FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    try
    {
        var startTime = DateTime.UtcNow;
        
        // Run a test prediction to measure response time
        var testResult = promptGuard.Predict("test");
        
        var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        if (promptGuard.IsUsingFallback)
        {
            return Results.Json(
                new 
                { 
                    status = "degraded",
                    modelLoaded = false,
                    modelReady = false,
                    usingFallback = true,
                    timestamp = DateTime.UtcNow,
                    responseTimeMs = Math.Round(responseTime, 2),
                    testPrediction = new
                    {
                        text = "test",
                        label = testResult.Label,
                        score = Math.Round(testResult.Score, 4)
                    },
                    fallbackInfo = new
                    {
                        method = "keyword-based-detection",
                        accuracy = "lower than ML model",
                        message = "Using simple keyword matching as fallback"
                    },
                    modelError = promptGuard.ModelError
                },
                statusCode: 200);
        }
        
        return Results.Ok(new 
        { 
            status = "healthy",
            modelLoaded = true,
            modelReady = true,
            usingFallback = false,
            timestamp = DateTime.UtcNow,
            responseTimeMs = Math.Round(responseTime, 2),
            testPrediction = new
            {
                text = "test",
                label = testResult.Label,
                score = Math.Round(testResult.Score, 4)
            },
            modelInfo = new
            {
                name = "deberta-v3-base-prompt-injection-v2",
                labels = new[] { "SAFE", "INJECTION" },
                maxSequenceLength = 512
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new 
            { 
                status = "unhealthy",
                modelLoaded = false,
                modelReady = false,
                usingFallback = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                stackTrace = ex.StackTrace
            },
            statusCode: 503);
    }
})
.WithName("DetailedHealthCheck")
.WithTags("Health")
.WithOpenApi()
.WithSummary("Detailed health check with performance metrics")
.WithDescription("Provides detailed health information including model performance and test prediction");

// Detect prompt injection endpoint
app.MapPost("/api/detect", (
    [FromBody] DetectRequest request,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromServices] PredictionLoggerService logger,
    HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { error = "Text is required" });
    }

    try
    {
        var startTime = DateTime.UtcNow;
        var result = promptGuard.Predict(request.Text);
        var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        var response = new DetectResponseWithMetadata
        {
            Label = result.Label,
            Score = result.Score,
            Scores = result.AllScores,
            IsSafe = result.Label == "SAFE",
            Text = request.Text,
            UsingFallback = promptGuard.IsUsingFallback,
            DetectionMethod = promptGuard.IsUsingFallback ? "keyword-fallback" : "ml-model"
        };

        // Log the prediction
        logger.LogPrediction(new PredictionLog
        {
            Text = request.Text,
            Label = result.Label,
            Score = result.Score,
            IsSafe = result.Label == "SAFE",
            AllScores = result.AllScores,
            ResponseTimeMs = responseTime,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UsedFallback = promptGuard.IsCurrentlyUsingFallback(),
            DetectionMethod = promptGuard.GetCurrentDetectionMethod(),
            ModelAvailable = promptGuard.IsModelLoaded
        });

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error processing request",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("DetectPromptInjection")
.WithTags("Detection")
.WithOpenApi()
.WithSummary("Detect prompt injection in text")
.WithDescription("Analyzes the provided text and determines if it contains a prompt injection attack");

// Batch detection endpoint
app.MapPost("/api/detect/batch", (
    [FromBody] List<DetectRequest> requests,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromServices] PredictionLoggerService logger,
    HttpContext httpContext) =>
{
    if (requests == null || requests.Count == 0)
    {
        return Results.BadRequest(new { error = "At least one request is required" });
    }

    try
    {
        var responses = requests.Select(request =>
        {
            var startTime = DateTime.UtcNow;
            var result = promptGuard.Predict(request.Text);
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // Log each prediction
            logger.LogPrediction(new PredictionLog
            {
                Text = request.Text,
                Label = result.Label,
                Score = result.Score,
                IsSafe = result.Label == "SAFE",
                AllScores = result.AllScores,
                ResponseTimeMs = responseTime,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UsedFallback = promptGuard.IsCurrentlyUsingFallback(),
                DetectionMethod = promptGuard.GetCurrentDetectionMethod(),
                ModelAvailable = promptGuard.IsModelLoaded
            });
            
            return new DetectResponse
            {
                Label = result.Label,
                Score = result.Score,
                Scores = result.AllScores,
                IsSafe = result.Label == "SAFE",
                Text = request.Text
            };
        }).ToList();

        return Results.Ok(responses);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Error processing batch request",
            detail: ex.Message,
            statusCode: 500);
    }
})
.WithName("DetectPromptInjectionBatch")
.WithTags("Detection")
.WithOpenApi()
.WithSummary("Detect prompt injection in multiple texts")
.WithDescription("Analyzes multiple texts and determines if they contain prompt injection attacks");

// Get prediction logs endpoint
app.MapGet("/api/logs", (
    [FromServices] PredictionLoggerService logger,
    [FromQuery] string? label = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 100) =>
{
    var logs = logger.GetLogs(label, skip, take);
    var totalCount = logger.GetCount();
    
    return Results.Ok(new
    {
        totalCount,
        skip,
        take,
        logs
    });
})
.WithName("GetPredictionLogs")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Get prediction logs")
.WithDescription("Retrieves logged predictions with optional filtering. Use 'label' query param to filter by SAFE or INJECTION.");

// Get prediction statistics endpoint
app.MapGet("/api/logs/stats", ([FromServices] PredictionLoggerService logger) =>
{
    var stats = logger.GetStatistics();
    return Results.Ok(stats);
})
.WithName("GetPredictionStatistics")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Get prediction statistics")
.WithDescription("Returns aggregated statistics about all predictions");

// Clear logs endpoint
app.MapDelete("/api/logs", ([FromServices] PredictionLoggerService logger) =>
{
    logger.ClearLogs();
    return Results.Ok(new { message = "All logs cleared successfully" });
})
.WithName("ClearPredictionLogs")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Clear all prediction logs")
.WithDescription("Removes all logged predictions from memory");

// Get model status endpoint
app.MapGet("/api/model/status", ([FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    return Results.Ok(new
    {
        modelLoaded = promptGuard.IsModelLoaded,
        modelEnabled = promptGuard.ForceUseModel,
        currentlyUsingFallback = promptGuard.IsCurrentlyUsingFallback(),
        detectionMethod = promptGuard.GetCurrentDetectionMethod(),
        modelError = promptGuard.ModelError,
        canToggle = promptGuard.IsModelLoaded,
        status = promptGuard.IsModelLoaded 
            ? (promptGuard.ForceUseModel ? "enabled" : "disabled")
            : "unavailable"
    });
})
.WithName("GetModelStatus")
.WithTags("Model")
.WithOpenApi()
.WithSummary("Get model status and configuration")
.WithDescription("Returns information about whether the ML model is loaded and enabled");

// Toggle model on/off endpoint
app.MapPost("/api/model/toggle", (
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromQuery] bool enable) =>
{
    if (!promptGuard.IsModelLoaded)
    {
        return Results.BadRequest(new 
        { 
            error = "Cannot toggle model - ML model is not loaded",
            reason = promptGuard.ModelError,
            currentState = "fallback-only"
        });
    }

    promptGuard.ForceUseModel = enable;

    return Results.Ok(new
    {
        success = true,
        modelEnabled = enable,
        detectionMethod = promptGuard.GetCurrentDetectionMethod(),
        message = enable 
            ? "ML model enabled - predictions will use DeBERTa model"
            : "ML model disabled - predictions will use keyword fallback"
    });
})
.WithName("ToggleModel")
.WithTags("Model")
.WithOpenApi()
.WithSummary("Enable or disable the ML model")
.WithDescription("Toggle between ML model and fallback detection. Use query param: ?enable=true or ?enable=false");

// Force switch to ML model
app.MapPost("/api/model/enable", ([FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    if (!promptGuard.IsModelLoaded)
    {
        return Results.BadRequest(new 
        { 
            error = "Cannot enable model - ML model is not loaded",
            reason = promptGuard.ModelError
        });
    }

    promptGuard.ForceUseModel = true;
    return Results.Ok(new
    {
        success = true,
        message = "ML model enabled",
        detectionMethod = "ml-model"
    });
})
.WithName("EnableModel")
.WithTags("Model")
.WithOpenApi()
.WithSummary("Enable ML model")
.WithDescription("Switch to using the ML model for predictions");

// Force switch to fallback
app.MapPost("/api/model/disable", ([FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    promptGuard.ForceUseModel = false;
    return Results.Ok(new
    {
        success = true,
        message = "ML model disabled - using fallback detection",
        detectionMethod = "keyword-fallback"
    });
})
.WithName("DisableModel")
.WithTags("Model")
.WithOpenApi()
.WithSummary("Disable ML model (force fallback)")
.WithDescription("Force the API to use keyword-based fallback detection instead of ML model");

app.Run();

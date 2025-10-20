using DebertaInferenceModel.Api.Configuration;
using DebertaInferenceModel.Api.Filters;
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
        Title = "Inference API", 
        Version = "v1",
        Description = """
The Inference API provides access to deployed AI models for performing predictions, classifications, or generative tasks.
It receives preprocessed input (tokens or text), runs the appropriate model (e.g. DeBERTa, CodeBERT), and returns structured outputs such as probabilities, labels, embeddings, or generated text.
This API acts as the central gateway for model execution within the AI platform.
"""
    });
    
    // Add operation filter to provide examples for model parameter
    c.OperationFilter<ModelParameterExamplesFilter>();
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

// Register ModelRegistryService as singleton
builder.Services.AddSingleton<ModelRegistryService>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inference API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Inference API";
});

app.UseHttpsRedirection();


// Get available models endpoint
app.MapGet("/api/models", ([FromServices] ModelRegistryService modelRegistry) =>
{
    var models = modelRegistry.GetAllModels();
    
    return Results.Ok(new
    {
        count = models.Count,
        models
    });
})
.WithName("GetAvailableModels")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Get list of available models")
.WithDescription("Returns a list of all available models that can be used for prompt injection detection");

// Get specific model information
app.MapGet("/api/models/{model}", (
    string model,
    [FromServices] ModelRegistryService modelRegistry) =>
{
    var modelInfo = modelRegistry.GetModelById(model);
    
    if (modelInfo == null)
    {
        return Results.NotFound(new 
        { 
            error = $"Model '{model}' not found",
            availableModels = modelRegistry.GetModelIds()
        });
    }
    
    return Results.Ok(modelInfo);
})
.WithName("GetModelById")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Get specific model information")
.WithDescription("Returns detailed information about a specific model by its ID");

// Helper method to validate model exists
IResult? ValidateModel(string model, ModelRegistryService modelRegistry)
{
    if (!modelRegistry.ModelExists(model))
    {
        return Results.NotFound(new
        {
            error = $"Model '{model}' not found",
            availableModels = modelRegistry.GetModelIds(),
            message = "Please use GET /api/models to see available models"
        });
    }
    return null;
}

// Helper method for health check logic
IResult HealthCheckLogic(PromptGuardServiceWrapper promptGuard, string model)
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
                    model,
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
            model,
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
                model,
                modelLoaded = false,
                modelReady = false,
                usingFallback = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message
            },
            statusCode: 503);
    }
}

// Health check endpoint with model parameter
app.MapGet("/api/models/{model}/health", (
    string model,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromServices] ModelRegistryService modelRegistry) =>
{
    var validationResult = ValidateModel(model, modelRegistry);
    if (validationResult != null) return validationResult;
    
    return HealthCheckLogic(promptGuard, model);
})
.WithName("HealthCheck")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Check API and model health")
.WithDescription("Verifies that the API is running and the specified ML model is loaded and ready");

// Helper method for detailed health check logic
IResult DetailedHealthCheckLogic(PromptGuardServiceWrapper promptGuard, string model)
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
                    model,
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
            model,
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
                name = model,
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
                model,
                modelLoaded = false,
                modelReady = false,
                usingFallback = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message,
                stackTrace = ex.StackTrace
            },
            statusCode: 503);
    }
}

// Detailed health check endpoint with model parameter
app.MapGet("/api/models/{model}/health/detailed", (string model, [FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    return DetailedHealthCheckLogic(promptGuard, model);
})
.WithName("DetailedHealthCheck")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Detailed health check with performance metrics")
.WithDescription("Provides detailed health information including model performance and test prediction for the specified model");

// Helper method for detection logic
IResult DetectPromptInjectionLogic(
    DetectRequest request,
    PromptGuardServiceWrapper promptGuard,
    PredictionLoggerService logger,
    HttpContext httpContext,
    string model)
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
            DetectionMethod = promptGuard.IsUsingFallback ? "keyword-fallback" : "ml-model",
            Model = model
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
            ModelAvailable = promptGuard.IsModelLoaded,
            Model = model
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
}

// Detect prompt injection endpoint with model parameter
app.MapPost("/api/models/{model}/detect", (
    string model,
    [FromBody] DetectRequest request,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromServices] PredictionLoggerService logger,
    [FromServices] ModelRegistryService modelRegistry,
    HttpContext httpContext) =>
{
    var validationResult = ValidateModel(model, modelRegistry);
    if (validationResult != null) return validationResult;
    
    return DetectPromptInjectionLogic(request, promptGuard, logger, httpContext, model);
})
.WithName("DetectPromptInjection")
.WithTags("Detection")
.WithOpenApi()
.WithSummary("Detect prompt injection in text")
.WithDescription("Analyzes the provided text using the specified model and determines if it contains a prompt injection attack. Requires 'text' in request body and 'model' in route parameter.");

// Helper method for batch detection logic
IResult BatchDetectPromptInjectionLogic(
    List<DetectRequest> requests,
    PromptGuardServiceWrapper promptGuard,
    PredictionLoggerService logger,
    HttpContext httpContext,
    string model)
{
    if (requests == null || requests.Count == 0)
    {
        return Results.BadRequest(new { error = "At least one request is required" });
    }

    // Validate all requests have required fields
    for (int i = 0; i < requests.Count; i++)
    {
        if (string.IsNullOrWhiteSpace(requests[i].Text))
        {
            return Results.BadRequest(new { error = $"Text is required for request at index {i}" });
        }
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
                ModelAvailable = promptGuard.IsModelLoaded,
                Model = model
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
}

// Batch detection endpoint with model parameter
app.MapPost("/api/models/{model}/detect/batch", (
    string model,
    [FromBody] List<DetectRequest> requests,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromServices] PredictionLoggerService logger,
    HttpContext httpContext) =>
{
    return BatchDetectPromptInjectionLogic(requests, promptGuard, logger, httpContext, model);
})
.WithName("DetectPromptInjectionBatch")
.WithTags("Detection")
.WithOpenApi()
.WithSummary("Detect prompt injection in multiple texts")
.WithDescription("Analyzes multiple texts using the specified model and determines if they contain prompt injection attacks. Each request requires 'text' field. Model is specified in route parameter.");

// Helper method for getting logs
IResult GetLogsLogic(
    PredictionLoggerService logger,
    string? model = null,
    string? label = null,
    int skip = 0,
    int take = 100)
{
    var logs = logger.GetLogs(label, skip, take);
    var totalCount = logger.GetCount();
    
    // Filter by model if specified
    if (!string.IsNullOrWhiteSpace(model))
    {
        logs = logs.Where(log => log.Model == model).ToList();
    }
    
    return Results.Ok(new
    {
        model = model ?? "all",
        totalCount,
        returnedCount = logs.Count(),
        skip,
        take,
        logs
    });
}

// Get prediction logs endpoint with model as query parameter
app.MapGet("/api/logs", (
    [FromServices] PredictionLoggerService logger,
    [FromQuery] string? model = null,
    [FromQuery] string? label = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 100) =>
{
    return GetLogsLogic(logger, model, label, skip, take);
})
.WithName("GetPredictionLogs")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Get prediction logs")
.WithDescription("Retrieves logged predictions with optional filtering. Use 'model' query param to filter by specific model, 'label' to filter by SAFE or INJECTION.");

// Helper method for getting statistics
IResult GetStatisticsLogic(PredictionLoggerService logger, string? model = null)
{
    var allLogs = logger.GetLogs(null, 0, int.MaxValue);
    
    // Filter by model if specified
    if (!string.IsNullOrWhiteSpace(model))
    {
        allLogs = allLogs.Where(log => log.Model == model).ToList();
    }
    
    var stats = new
    {
        totalPredictions = allLogs.Count(),
        safeCount = allLogs.Count(l => l.IsSafe),
        injectionCount = allLogs.Count(l => !l.IsSafe),
        avgResponseTimeMs = allLogs.Any() ? Math.Round(allLogs.Average(l => l.ResponseTimeMs), 2) : 0,
        fallbackUsageCount = allLogs.Count(l => l.UsedFallback),
        modelUsageCount = allLogs.Count(l => !l.UsedFallback),
        byModel = allLogs.GroupBy(l => l.Model).Select(g => new
        {
            model = g.Key,
            count = g.Count(),
            safeCount = g.Count(l => l.IsSafe),
            injectionCount = g.Count(l => !l.IsSafe)
        }).ToList()
    };
    
    return Results.Ok(new 
    { 
        model = model ?? "all",
        stats 
    });
}

// Get prediction statistics endpoint with model as query parameter
app.MapGet("/api/logs/stats", (
    [FromServices] PredictionLoggerService logger,
    [FromQuery] string? model = null) =>
{
    return GetStatisticsLogic(logger, model);
})
.WithName("GetPredictionStatistics")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Get prediction statistics")
.WithDescription("Returns aggregated statistics about predictions. Use 'model' query param to filter statistics for a specific model.");

// Helper method for clearing logs
IResult ClearLogsLogic(PredictionLoggerService logger)
{
    logger.ClearLogs();
    var message = "All prediction logs cleared successfully";
    return Results.Ok(new { message });
}

// Clear logs endpoint
app.MapDelete("/api/logs", ([FromServices] PredictionLoggerService logger) =>
{
    return ClearLogsLogic(logger);
})
.WithName("ClearPredictionLogs")
.WithTags("Logs")
.WithOpenApi()
.WithSummary("Clear all prediction logs")
.WithDescription("Removes all logged predictions from memory across all models");

// Helper method for getting model status
IResult GetModelStatusLogic(PromptGuardServiceWrapper promptGuard, string model)
{
    return Results.Ok(new
    {
        model,
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
}

// Get model status endpoint with model parameter
app.MapGet("/api/models/{model}/status", (string model, [FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    return GetModelStatusLogic(promptGuard, model);
})
.WithName("GetModelStatus")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Get model status and configuration")
.WithDescription("Returns information about whether the specified ML model is loaded and enabled");

// Helper method for toggling model
IResult ToggleModelLogic(PromptGuardServiceWrapper promptGuard, bool enable, string model)
{
    if (!promptGuard.IsModelLoaded)
    {
        return Results.BadRequest(new 
        { 
            model,
            error = "Cannot toggle model - ML model is not loaded",
            reason = promptGuard.ModelError,
            currentState = "fallback-only"
        });
    }

    promptGuard.ForceUseModel = enable;

    return Results.Ok(new
    {
        model,
        success = true,
        modelEnabled = enable,
        detectionMethod = promptGuard.GetCurrentDetectionMethod(),
        message = enable 
            ? "ML model enabled - predictions will use DeBERTa model"
            : "ML model disabled - predictions will use keyword fallback"
    });
}

// Toggle model on/off endpoint with model parameter
app.MapPost("/api/models/{model}/toggle", (
    string model,
    [FromServices] PromptGuardServiceWrapper promptGuard,
    [FromQuery] bool enable) =>
{
    return ToggleModelLogic(promptGuard, enable, model);
})
.WithName("ToggleModel")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Enable or disable the ML model")
.WithDescription("Toggle between ML model and fallback detection for the specified model. Use query param: ?enable=true or ?enable=false");

// Helper method for enabling model
IResult EnableModelLogic(PromptGuardServiceWrapper promptGuard, string model)
{
    if (!promptGuard.IsModelLoaded)
    {
        return Results.BadRequest(new 
        { 
            model,
            error = "Cannot enable model - ML model is not loaded",
            reason = promptGuard.ModelError
        });
    }

    promptGuard.ForceUseModel = true;
    return Results.Ok(new
    {
        model,
        success = true,
        message = "ML model enabled",
        detectionMethod = "ml-model"
    });
}

// Force switch to ML model with model parameter
app.MapPost("/api/models/{model}/enable", (string model, [FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    return EnableModelLogic(promptGuard, model);
})
.WithName("EnableModel")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Enable ML model")
.WithDescription("Switch to using the specified ML model for predictions");

// Helper method for disabling model
IResult DisableModelLogic(PromptGuardServiceWrapper promptGuard, string model)
{
    promptGuard.ForceUseModel = false;
    return Results.Ok(new
    {
        model,
        success = true,
        message = "ML model disabled - using fallback detection",
        detectionMethod = "keyword-fallback"
    });
}

// Force switch to fallback with model parameter
app.MapPost("/api/models/{model}/disable", (string model, [FromServices] PromptGuardServiceWrapper promptGuard) =>
{
    return DisableModelLogic(promptGuard, model);
})
.WithName("DisableModel")
.WithTags("Models")
.WithOpenApi()
.WithSummary("Disable ML model (force fallback)")
.WithDescription("Force the API to use keyword-based fallback detection instead of the specified ML model");

app.Run();

using DebertaInferenceModel.Api.Services;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DebertaInferenceModel.Api.Filters;

/// <summary>
/// Swagger operation filter to add examples and descriptions for model parameters
/// </summary>
public class ModelParameterExamplesFilter : IOperationFilter
{
    private readonly ModelRegistryService _modelRegistry;

    public ModelParameterExamplesFilter(ModelRegistryService modelRegistry)
    {
        _modelRegistry = modelRegistry;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Find the model parameter in the route
        if (operation.Parameters != null)
        {
            var modelParam = operation.Parameters.FirstOrDefault(p => 
                p.Name == "model" && p.In == ParameterLocation.Path);
            
            if (modelParam != null)
            {
                var modelIds = _modelRegistry.GetModelIds();
                var modelList = string.Join("\n- ", modelIds);
                var firstModel = modelIds.FirstOrDefault() ?? "model-id";
                
                var description = modelIds.Count == 1
                    ? $@"Model identifier to use for the operation. 
                
Available model:
- {modelList}

Use GET /api/models to see full model details."
                    : $@"Model identifier to use for the operation. 
                
Available models:
- {modelList}

Use GET /api/models to see full model details.";
                
                modelParam.Description = description;
                modelParam.Example = new Microsoft.OpenApi.Any.OpenApiString(firstModel);
                
                // Add schema with examples
                if (modelParam.Schema != null)
                {
                    modelParam.Schema.Example = new Microsoft.OpenApi.Any.OpenApiString(firstModel);
                }
            }
        }
    }
}


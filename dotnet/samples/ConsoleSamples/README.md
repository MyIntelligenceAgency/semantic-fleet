# Semantic Kernel syntax examples

This project contains a collection of semi-random examples about various scenarios
using SK components.

The examples are ordered by number, starting with very basic examples.

## Running Examples with Filters

You can run individual examples in the KernelSyntaxExamples project using various methods to specify a filter. This allows you to execute specific examples without running all of them. Choose one of the following options to apply a filter:

### Option 1: Set the Default Filter in Program.cs

In your code, you can set a default filter by modifying the appropriate variable or parameter. Look for the section in your code where the filter is applied or where the examples are defined, and change the filter value accordingly.

```csharp
// Example of setting a default filter in code
string defaultFilter = "Example0"; // will run all examples that contain 'example0' in the name
```

### Option 2: Set Command-Line Arguments
Right-click on your console application project in the Solution Explorer.

Choose "Properties" from the context menu.

In the project properties window, navigate to the "Debug" tab on the left.

Supply Command-Line Arguments:

In the "Command line arguments" field, enter the command-line arguments that your console application expects. Separate multiple arguments with spaces.

### Option 3: Use Visual Studio Code Filters
If you are using Visual Studio Code, you can specify a filter using the built-in filter options provided by the IDE. These options can be helpful when running your code in a debugging environment. Consult the documentation for Visual Studio Code or the specific extension you're using for information on applying filters.

### Option 4: Modify launch.json
If you are using Visual Studio or a similar IDE that utilizes launch configurations, you can specify the filter in your launch.json configuration file. Edit the configuration for your project to include the filter parameter.


## Configuring Secrets
Most of the examples will require secrets and credentials, to access OpenAI, Azure OpenAI,
Bing and other resources. We suggest using .NET
[Secret Manager](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
to avoid the risk of leaking secrets into the repository, branches and pull requests.
You can also use environment variables if you prefer.

To set your secrets with Secret Manager:
```
cd dotnet/samples/KernelSyntaxExamples

dotnet user-secrets init

dotnet user-secrets set "OpenAI:ModelId" "..."
dotnet user-secrets set "OpenAI:ChatModelId" "..."
dotnet user-secrets set "OpenAI:EmbeddingModelId" "..."
dotnet user-secrets set "OpenAI:ApiKey" "..."

dotnet user-secrets set "Oobabooga:EndPoint" "..."
dotnet user-secrets set "Oobabooga:BlockingPort" "..."
dotnet user-secrets set "Oobabooga:StreamingPort" "..."


```

To set your secrets with environment variables, use these names:
```
# OpenAI
OpenAI__ModelId
OpenAI__ChatModelId
OpenAI__EmbeddingModelId
OpenAI__ApiKey

# Oobabooga
Oobabooga__EndPoint
Oobabooga__BlockingPort
Oobabooga__StreamingPort


```

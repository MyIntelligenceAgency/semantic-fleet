## MultiConnector Integration Tests

This directory houses integration tests for the MultiConnector, which are tailored to ensure the MultiConnector's accurate operation comprehensively.


### What is the MultiConnector?

The MultiConnector serves as an AI completion provider for the semantic-kernel. It integrates various connectors and directs completion calls to the suitable connector based on its capabilities.

### What is the MultiConnector Test?

This test measures the performance of different text generation software connectors, focusing on their response times. By conducting this test, users can discern which connector aligns best with their requirements in terms of efficiency and dependability.

For the purposes of the test, ChatGPT acts as the primary connector for generating samples while also evaluating secondary connectors on identical tasks. The secondary connectors are smaller LLama 2 models self-hosted using the open-source application, oobabooga.


### Directory Structure

- **Plans**: Contains JSON plans for the connectors.
- **Texts**: Contains text files which can be used during testing.

**Primary File** :

- `MultiConnectorTests.cs` - Contains the integration test cases for the MultiConnector.


### Setting Up Oobabooga & Configuring Multi-start scripts:

For setting up Oobabooga and downloading models, please refer to the [Installing Oobabooga and Configuring Multi-Start Scripts](../../../../../docs/OOBABOOGA.md) guide.

Once configured, launch your multi-model environment.

### **Sync Your Settings**:
   The settings file appears as shown below. For these tests, the primary connector will be OpenAI. Ensure you've configured the respective settings or user secrets accurately.

   To toggle secondary connectors on or off, adjust the IncludedConnectors section by commenting or uncommenting lines.

   **Note**: Using an extra testsettings.development.json file to leave the main one intact? Employ the IncludedConnectorsDev section, which takes precedence over the IncludedConnectors section. 
   
   If you've modified models in your multi-start script, reflect those changes in the OobaboogaCompletions section of the main settings file.

   Also, most models were trained with a specific chat-instruct format, so those were included in the default settings in order to wrap call prompts. Additional global tokens are available in the settings files to fine tune  bindings between semantic function prompts and chat-instruct templates.

```json
{
  "OpenAI": { ... },
  "AzureOpenAI": { ... },
  "OpenAIEmbeddings": { ... },
  "AzureOpenAIEmbeddings": { ... },
  "HuggingFace": { ... },
  "Bing": { ... },
  "Postgres": { ... },
  "MultiConnector": {
    "OobaboogaEndPoint": "http://localhost",
    "GlobalParameters": {
      "SystemSupplement": "User is now playing a game where he is writing messages in the form of semantic functions. That means you are expected to strictly answer with a completion of his message, without adding any additional comments.",
      "UserPreamble": "Let's play a game: please read the following instructions, and simply answer with a completion of my message, don't add any personal comments."
    },
    "IncludedConnectors": [
      ...
    ],
    "IncludedConnectorsDev": [
      // Toggle models for development/testing without altering the main settings:
      //"TheBloke_orca_mini_3B-GGML",
      //"togethercomputer_RedPajama-INCITE-Chat-3B-v1",
      ...
    ],
    "OobaboogaCompletions": [
      {
        "Name": "TheBloke_orca_mini_3B-GGML",
        ...
      },
      ...
    ]
  }
}
```


### **Initiate Your Tests**:
   
   Integration tests are off by default. To activate, switch [Theory(Skip = "This test is for manual verification.")] to [Theory].

   **Pick a Test**:
   The length of tests can vary based on parameter intricacy. Running tests separately through your IDE is preferable over the complete suite.

Each test follows this workflow:

    - Initialization
        - MultiCompletion settings are initialized from global parameters, according to the models you have activated in the settings file.
        - A kernel is initialized with skills and multicompletion settings
        - A plan with one or several semantic functions is generated from a factory
    - The plan is run once. The primary connector defined (Chat GPT) is used to generate the various completions.
        - Performance in cost and in duration is recorded.
        - Samples are collected automatically during the run
        - Result of the plan is shown.
    - An analysis task is run from samples collected during the run.
        - Each connector is tested on the samples.
        - The primary connector (ChatGPT) evaluates the test runs, vetting each connector's capability to handle each corresponding prompt type.
        - New settings are computed from the evaluation. Vetted connectors are promoted to handle the corresponding prompt types.
        - MultiCompletion settings are updated according to the analysis results.
    - The original plan is reloaded and run again. This time, the secondary connectors may be used to generate some or all of the completions according to the updated settings.
        - Performance in cost and in duration is recorded.
        - Result of the plan is shown
    - A third instance of a plan is generated with distinct data.
        - New plan is run with same settings
        - New samples are collected automatically during the run
        - Samples are evaluated by the primary connector
    - Asserts are run on the results to validate the test, which succeeds iif:
        - Some cost performance were observed between 1st and 2nd run (at least a secondary connector was vetted once)
        - Validation samples belonging to secondary connectors are all vetted by the primary connector, with at least one sample validated.


   There are 2 kinds of plan factories available: 

   - Static plans are loaded from .json files the Plan folder. They are injected with test and validation texts of various complexities from .txt files in the Text folder.
        - ChatGptOffloadsToMultipleOobaboogaUsingFileAsync loads a multiCompletion with all tests enabled in the settings, and will fail if the corresponding models are not loaded. 
        - ChatGptOffloadsToSingleOobaboogaUsingFileAsync will test a single model, and will succeed if the corresponding model is not loaded.
   - Dynamic plans are generated by calling primary connector with a sequential planner. Those plans are more variable and test's success rate is variable. 
        - ChatGptOffloadsToOobaboogaUsingPlannerAsync is the entry point for those tests.

 9. **Gather and Examine Execution Trace**:

    Tests produce extensive log traces detailing all stages and results. For better clarity, transfer the trace to a markdown viewer.
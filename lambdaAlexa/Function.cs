using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Alexa.NET.Response;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using System.Net.Http;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace lambdaAlexa
{
    public class Function
    {
        private static HttpClient _httpClient;
        public const string INVOCATION_NAME = "Version Checker";
        public Function()
        {

            _httpClient = new HttpClient();
        }

        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {

            var requestType = input.GetRequestType();
   

            if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                var projectRequested = String.Empty;
                var projectRegisteredinVUI = String.Empty;

                try
                {
                    projectRequested = intentRequest.Intent.Slots["Project"].Resolution.Authorities[0].Values[0].Value.Id;
                    projectRegisteredinVUI = intentRequest.Intent.Slots["Project"].Value;
                    context.Logger.LogLine("Resolving to: " + intentRequest.Intent.Slots["Project"].Resolution.Authorities[0].Values[0].Value.Id);
                }catch(Exception ex)
                {
                    context.Logger.LogLine("Error in parsing Value ID");
                    return MakeSkillResponse(" Sorry, I might not support that project yet. But try asking another project or to exit, say exit", true);
                }


                if (projectRequested.Length<2)
                {
                    context.Logger.LogLine("Project name is empty");
                    return MakeSkillResponse("I cannot Parse the Project you asked. Sorry for the Inconvinience", false);
                }
                if (String.IsNullOrEmpty(projectRequested))
                {
                    context.Logger.Log("Project name is empty");
                    return MakeSkillResponse("I am sorry. I did not understand the Project you mentioned. Please ask again.", false);
                }

                var projectlatestVersion = await GetProjectInfo(projectRequested, context);

                //TODO: output text can be "Not Found"

                var latestnumber = projectlatestVersion.latest;
                
                var outputText = $"Hmmm. Latest Stable version for {projectRegisteredinVUI} is {latestnumber.Replace(".", " Point ")}";

                return MakeSkillResponse(outputText, true);

            }
            else
            {
                return MakeSkillResponse(
                        $"I don't understand what you said. Please say something like Alexa, ask {INVOCATION_NAME} the latest version of Python.",
                        true);
            }
        }


        private SkillResponse MakeSkillResponse(string outputSpeech,
            bool shouldEndSession,
            string repromptText = "Just say, latest version of followed by name of Project. To exit, say, exit.")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillResponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };
            return skillResponse;
        }


        private async Task<Project> GetProjectInfo(string projectRequested, ILambdaContext context)
        {
            var uri = new Uri($"https://verse.pawelad.xyz/projects/{projectRequested}/?format=json");
            context.Logger.LogLine("Attempting to connect to " + uri);
            Project projectRequestedJson = new Project();
            try
            {
                string jsonResponse = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{jsonResponse}");
                projectRequestedJson = JsonConvert.DeserializeObject<Project>(jsonResponse);

            }
            catch(Exception ex)
            {
                context.Logger.LogLine("Error in connecting to API server " + ex.StackTrace);
            }

            context.Logger.LogLine("Deserialized it to : " + projectRequestedJson.latest);
            return projectRequestedJson;
        }


    }

    #region Deserialization classes
    public class Project
    {
        public string latest { get; set; }
    }

    #endregion

}

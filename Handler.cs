using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Redirector
{
    public class Function
    {
        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
        });
        private static readonly List<string> bodyMethods = new List<string>
        {
            "POST",
            "PATCH",
            "PUT"
        };

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            string getParams = "?";
            if (request.QueryStringParameters != null)
            {
                foreach (var param in request.QueryStringParameters)
                {
                    getParams += param.Key;
                    getParams += "=";
                    getParams += param.Value;
                    getParams += "&";
                }
            }
            var teamserver = Environment.GetEnvironmentVariable("teamserver");
            var uri = new Uri("https://" + teamserver + "/" + request.RequestContext.Stage + request.Path + getParams);


            
            var newRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), uri);
            newRequest.Method = new HttpMethod(request.HttpMethod);
            if (bodyMethods.Any(x => x.Contains(request.HttpMethod)))
            {
                System.Console.WriteLine("BODY REQUEST");
                newRequest.Content = new StringContent(request.Body);
            }

            foreach (var header in request.Headers)
            {
                Console.WriteLine($"{header.Key}, {header.Value}");
                newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }            
            
            Console.WriteLine(uri.ToString());
            var response = await Client.SendAsync(newRequest);
            var outBoundHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                outBoundHeaders.TryAdd(header.Key,header.Value.FirstOrDefault());
            }
                      
            
            return new APIGatewayProxyResponse
            {
                StatusCode = Convert.ToInt32(response.StatusCode),
                Body = await response.Content.ReadAsStringAsync(),
                Headers = outBoundHeaders

            };
        
            
        }
    }
}

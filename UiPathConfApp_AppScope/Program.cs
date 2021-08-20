using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UiPathConfApp_AppScope
{
    class Program
    {
        const string AuthorizationEndpoint = "https://cloud.uipath.com/identity_/connect/token";
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Required command line arguments: client-id client-secret");
                return 1;
            }
            string clientId = args[0];
            string clientSecret = args[1];

            Program p = new Program();
            await p.DoOAuthAsync(clientId, clientSecret);
            return 0;
        }

        private async Task DoOAuthAsync(string clientId, string clientSecret)
        {
            const string scope = "OR.Folders";
            string tokenRequestBody = string.Format("client_id={0}&client_secret={1}&scope={2}&grant_type=client_credentials",
                clientId,
                clientSecret,
                scope
                );
            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create(AuthorizationEndpoint);
            tokenRequest.Method = "POST";
            tokenRequest.ContentType = "application/x-www-form-urlencoded";
            tokenRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            byte[] tokenRequestBodyBytes = Encoding.ASCII.GetBytes(tokenRequestBody);
            tokenRequest.ContentLength = tokenRequestBodyBytes.Length;

            using (Stream requestStream = tokenRequest.GetRequestStream())
            {
                await requestStream.WriteAsync(tokenRequestBodyBytes, 0, tokenRequestBodyBytes.Length);
            }

            try
            {
                // gets the response
                WebResponse tokenResponse = await tokenRequest.GetResponseAsync();
                using (StreamReader reader = new StreamReader(tokenResponse.GetResponseStream()))
                {
                    // reads response body
                    string responseText = await reader.ReadToEndAsync();
                    Console.WriteLine(responseText);

                    // converts to dictionary
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

                    string accessToken = tokenEndpointDecoded["access_token"];
                    await RequestFolderInfoAsync(accessToken);
                    Console.WriteLine(accessToken); 
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        Log("HTTP: " + response.StatusCode);
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            // reads response body
                            string responseText = await reader.ReadToEndAsync();
                            Log(responseText);
                        }
                    }

                }
            }
        }

        private void Log(string output)
        {
            Console.WriteLine(output);
        }

        private async Task RequestFolderInfoAsync(string accessToken)
        {
            Log("Making API Call to Folderinfo...");

            // builds the  request
            string folderinfoRequestUri = "https://cloud.uipath.com/regisagedcare/Production/orchestrator_/odata/Folders";

            // sends the request
            HttpWebRequest folderinfoRequest = (HttpWebRequest)WebRequest.Create(folderinfoRequestUri);
            folderinfoRequest.Method = "GET";
            folderinfoRequest.Headers.Add(string.Format("Authorization: Bearer {0}", accessToken));
            folderinfoRequest.ContentType = "application/x-www-form-urlencoded";
            folderinfoRequest.Accept = "Accept=text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // gets the response
            WebResponse folderinfoResponse = await folderinfoRequest.GetResponseAsync();
            using (StreamReader userinfoResponseReader = new StreamReader(folderinfoResponse.GetResponseStream()))
            {
                // reads response body
                string folderinfoResponseText = await userinfoResponseReader.ReadToEndAsync();
                Log(folderinfoResponseText);
            }
        }
    }
}

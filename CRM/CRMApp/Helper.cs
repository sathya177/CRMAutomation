using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CRMApp
{
    public class Helper
    {
        

        public static string GetParameterValueFromConnectionString(string connectionString, string parameter)
        {
            try
            {
                return connectionString.Split(';').Where(s => s.Trim().StartsWith(parameter)).FirstOrDefault().Split('=')[1];
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }

        public static HttpClient GetHttpClient(string connectionString,string version)
        {
            string url = GetParameterValueFromConnectionString(connectionString, "Url");
            string username = GetParameterValueFromConnectionString(connectionString, "Username");
            string domain = GetParameterValueFromConnectionString(connectionString, "Domain");
            string password = GetParameterValueFromConnectionString(connectionString, "Password");
            string authType = GetParameterValueFromConnectionString(connectionString, "authtype");
          
            try
            {
                HttpMessageHandler messageHandler;

                switch (authType)
                {
                    case "Office365":
                    case "IFD":

                        messageHandler = new OAuthMessageHandler(url, "51f81489-12ee-4a9e-aaae-a2591f45987d", "app://58145B91-0C36-4500-8554-080854F2AC97", username, password,
                                 new HttpClientHandler());
                        break;
                    case "AD":
                        NetworkCredential credentials = new NetworkCredential(username, password, domain);
                        messageHandler = new HttpClientHandler() { Credentials = credentials };
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Valid authType values are 'Office365', 'IFD', or 'AD'.");

                }

                HttpClient httpClient = new HttpClient(messageHandler)
                {
                    BaseAddress = new Uri(string.Format("{0}/api/data/{1}/", url, version)),

                    Timeout = new TimeSpan(0, 10, 0)  //10 minutes
                };
                
                return httpClient;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary> Displays exception information to the console. </summary>
        /// <param name="ex">The exception to output</param>
        public static void DisplayException(Exception ex)
        {
            Console.WriteLine("The application terminated with an error.");
            Console.WriteLine(ex.Message);
            while (ex.InnerException != null)
            {
                Console.WriteLine("\t* {0}", ex.InnerException.Message);
                ex = ex.InnerException;
            }
        }
    }
}

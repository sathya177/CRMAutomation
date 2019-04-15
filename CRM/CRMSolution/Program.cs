using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CRMSolution
{
    class Program
    {
        static void Main(string[] args)
        {
            
                string srcConnectionString = ConfigurationManager.ConnectionStrings[args[0]].ConnectionString;
                string paths = ConfigurationManager.AppSettings["path"].ToString();
                using (HttpClient client = Helper.GetHttpClient(
                srcConnectionString,
                "v9.0"))
                {
                    RetrieveSolutions(client, paths);
                    Console.WriteLine("Connection Established");
                    return;
                }
           
        }
        static void RetrieveSolutions(HttpClient client, string path)
        {
            string queryOptions = "solutions?$select=friendlyname,uniquename,version,versionnumber,ismanaged";
            var response = client.GetAsync(queryOptions, HttpCompletionOption.ResponseHeadersRead).Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                JObject solutionArray = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                foreach (var item in solutionArray)
                {
                    Console.WriteLine(item.Key);
                    if (item.Key == "value")
                    {
                        Console.WriteLine(item.Value);
                        File.WriteAllText(path + "\\solutions.txt", item.Value.ToString());
                    }
                }

            }
            else
            { throw new Exception(string.Format("Failed to get  solutionID", response.Content)); }

        }
    }
}

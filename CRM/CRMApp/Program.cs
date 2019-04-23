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
using Newtonsoft.Json;
using System.Threading;

namespace CRMApp
{
    class Program
    {
        public static string GetSoltnWithVersion(string soltnname, string env, string path)
        {
            try
            {
                string version = string.Empty;
                var jsonData = JsonConvert.DeserializeObject<List<Class1>>(File.ReadAllText(path + "\\solutions.txt")).ToList();
                return soltnname + "_" + jsonData.Where(s => s.uniquename == soltnname).Select(n => n.version).FirstOrDefault().Replace(".", "_") + ".zip";
            }
            catch(Exception ex)
            {
                return "";
            }

            
        }
        public static void ProcessSolution(HttpClient client,string action, string source, string[] solutions, string path,Payload payload)
        {
            try
            {
                HttpRequestMessage request;
                HttpResponseMessage response;

                Console.WriteLine("List of Solutions to be  " + action + "ed ...");
                foreach (var item in solutions)
                {
                    Console.WriteLine("Solution :" + item);
                    try
                    {
                        if (action == "Export")
                        {
                            CancellationTokenSource cts = new CancellationTokenSource(60000); // 2 seconds
                            JObject expParames = new JObject();
                            expParames["SolutionName"] = item;
                            expParames["Managed"] = payload.managed;
                            request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress + "ExportSolution");

                            request.Content = new StringContent(expParames.ToString(), Encoding.UTF8, "application/json");
                            var resp = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result;
                            Console.WriteLine(action + " of " + item + " Started...");
                            if (resp.IsSuccessStatusCode)
                            {
                                Console.WriteLine(action + " of " + item + "In Progress...");
                                JObject responseData = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                                var d = responseData["ExportSolutionFile"];
                                Console.WriteLine(path + "\\" + GetSoltnWithVersion(item, source, path));
                                File.WriteAllBytes(path + "\\" + GetSoltnWithVersion(item, source, path), Convert.FromBase64String(d.ToString()));
                                Console.WriteLine(action + " of " + GetSoltnWithVersion(item, source, path) + " Completed Succesfully...");
                            }
                            else
                            {
                                Console.WriteLine(action + " of " + GetSoltnWithVersion(item, source, path) + "failed...");
                                Console.WriteLine("Error : ");
                                Console.WriteLine("Status code :" + resp.StatusCode);
                                Console.WriteLine("Reason Phrase :" + resp.ReasonPhrase);
                                Console.WriteLine(resp.Content);
                                throw new Exception();

                            }
                        }
                        else if (action == "Import")
                        {
                            JObject importParams = new JObject();
                            importParams["CustomizationFile"] = File.ReadAllBytes(path + "\\" + GetSoltnWithVersion(item, source, path));
                            importParams["OverwriteUnmanagedCustomizations"] = payload.overwritten;
                            importParams["PublishWorkflows"] = false;
                            importParams["ImportJobId"] = Guid.NewGuid();
                            request = new HttpRequestMessage(HttpMethod.Post, "ImportSolution");
                            request.Content = new StringContent(importParams.ToString(), Encoding.UTF8, "application/json");
                            var resp = client.SendAsync(request, HttpCompletionOption.ResponseContentRead).Result;
                            Console.WriteLine(action + " of " + item + " Started...");
                            if (resp.IsSuccessStatusCode)
                            {
                                Console.WriteLine(action + " of " + item + "In Progress...");
                                Console.WriteLine(action + " of " + item + " Completed Succesfully...");
                            }
                            else
                            {
                                Console.WriteLine(action + " of " + item + "failed...");
                                Console.WriteLine("Error : ");
                                Console.WriteLine("Status code : " + resp.StatusCode);
                                Console.WriteLine("Reason Phrase : " + resp.ReasonPhrase);

                                JObject responseData = JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                                Console.WriteLine(JsonConvert.SerializeObject(responseData));
                                throw new Exception();


                            }
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in " + item + " " + action);
                        Console.WriteLine(ex.Message);
                        File.WriteAllText(path + "\\result.txt", "success");
                    }
                }
            }
            catch(Exception ex)
            {
                File.WriteAllText(path + "\\result.txt", "success");
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
            {
                return;
            }

        }
        static void Main(string[] args)
        {

            try
            {

                var jsonInfo = JsonConvert.DeserializeObject<Payload>(args[0]);
                string source = jsonInfo.source;
                Console.WriteLine("Source Environment : " + source);
                string[] targets = jsonInfo.targets;
                string[] solutions = jsonInfo.solutions;

                //Get configuration data from App.config connectionStrings
                string sourceConnectionString = ConfigurationManager.ConnectionStrings[source].ConnectionString;
                string path = ConfigurationManager.AppSettings["path"].ToString();

                Console.WriteLine("Instance : " + Helper.GetParameterValueFromConnectionString(sourceConnectionString, "Url"));
                Console.WriteLine(" Connecting ...");
                using (HttpClient client = Helper.GetHttpClient(
                    sourceConnectionString,
                    "v9.0"))
                {
                    RetrieveSolutions(client, path);
                    Console.WriteLine(" Connection Established");
                    ProcessSolution(client, "Export", source, solutions, path, jsonInfo);
                }
                foreach (var target in targets)
                {
                    string targetConnectionString = ConfigurationManager.ConnectionStrings[target].ConnectionString;
                    Console.WriteLine("Retrieving Target env :" + target);
                    Console.Write("Instance : " + Helper.GetParameterValueFromConnectionString(targetConnectionString, "Url"));
                    Console.WriteLine(" Connecting ...");
                    using (HttpClient client = Helper.GetHttpClient(
                        targetConnectionString,
                        "v9.0"))
                    {
                        Console.WriteLine(" Connection Established");
                        ProcessSolution(client, "Import", source, solutions, path, jsonInfo);
                    }
                }
            }
            catch(Exception ex)
            {
                return;
            }
            
        }
    }
}

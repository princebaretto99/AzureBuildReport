using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;


namespace AzureBuildReport
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var BuildId = "";
            var Limit = 100;
            var Offset = 0;

            string[] Lines =
                {
            "<!DOCTYPE html><html><head><style>table {  font-family: arial, sans - serif; border - collapse: collapse; width: 100 %;}td, th; border: 1px solid #dddddd;  text-align: left;  padding: 8px;}</style></head><body><table>",
            "<tr><th>Build Name</th><th>Project Name</th><th>Session Name</th><th>Browser Name</th><th>Browser Version</th><th>OS Name</th><th>OS Version</th><th>Device</th><th>Status</tr>"

        };



            await System.IO.File.WriteAllLinesAsync("./output.html", Lines);

            var userName = Environment.GetEnvironmentVariable("BROWSERSTACK_USERNAME");
            var accessKey = Environment.GetEnvironmentVariable("BROWSERSTACK_ACCESS_KEY");
            var buildName = Environment.GetEnvironmentVariable("BROWSERSTACK_BUILD_NAME");


            var client = new RestClient("https://api.browserstack.com");
            client.Authenticator = new HttpBasicAuthenticator(userName, accessKey);
            Console.WriteLine("username: "+userName.Length);
            var buildApiRequest = new RestRequest("https://api.browserstack.com/automate/builds.json?limit=40");
            var buildQueryResult = await client.ExecuteAsync(buildApiRequest);
            var buildJsonStr = buildQueryResult.Content ?? "";

            if (!buildJsonStr.Equals(""))
            {
                var buildList = JArray.Parse(buildJsonStr);
                if (buildList.ToString().Length != 0)
                {

                    foreach (JObject buildObj in buildList)
                    {
                        var build = buildObj["automation_build"];
                        var bName = (string?)build["name"];
                        if (buildName.Equals(bName))
                        {
                            BuildId = build["hashed_id"].ToString();
                            break;
                        }
                    }
                }

                Console.WriteLine("Here:  "+BuildId);
            }

            if (BuildId.Equals(""))
            {
                Console.Write("Could not find a Build");

            }
            else
            {
                do
                {
                    var request = new RestRequest("/automate/builds/" + BuildId + "/sessions.json?limit=" + Limit + "&offset=" + Offset, Method.Get);
                    var queryResult = await client.ExecuteAsync(request);
                    var jsonStr = queryResult.Content ?? "";

                    if (jsonStr.Length > 2)
                    {
                        var sessionList = JArray.Parse(jsonStr);
                        if (sessionList.ToString().Length != 0)
                        {


                            foreach (JObject sessionObj in sessionList)
                            {

                                if (sessionObj != null)
                                {
                                    var session = sessionObj["automation_session"];
                                    var browserUrl = session["browser_url"].ToString().Replace("/builds", "/dashboard/v2/builds");
                                    var sessionName = session["name"].ToString().Length == 0 ? session["name"] : session["hashed_id"];

                                    string[] textContextToAdd = { "<tr><td>" + session["build_name"].ToString() +
                        "</td><td>" + session["project_name"] +
                        "</td><td><a href=" + browserUrl + ">" + sessionName + "</a>" +
                        "</td><td>" + session["browser"] +
                        "</td><td>" + session["browser_version"] +
                        "</td><td>" + session["os"] +
                        "</td><td>" + session["os_version"] +
                        "</td><td>" + session["device"] +
                        "</td><td>" + session["status"] + "</tr>" };
                                    await System.IO.File.AppendAllLinesAsync("./output.html", textContextToAdd);
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }

                    Offset += Limit;
                } while (true);

            }

        }

    }
}

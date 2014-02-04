using System;
using System.Collections.Generic;
using AlteryxGalleryAPIWrapper;
using HtmlAgilityPack;
using Newtonsoft.Json;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace PredictiveInventoryAnalysis
{
    [Binding]
    public class PredictiveInventoryAnalysisSteps
    {
        private string alteryxurl;
        private string _sessionid;
        private string _appid;
        private string _userid;
        private string _appName;
        private string jobid;
        private string outputid;
        private string validationId;
        private string _appActualName;


        private Client Obj = new Client("https://gallery.alteryx.com/api");


        private RootObject jsString = new RootObject();

        [Given(@"alteryx running at""(.*)""")]
        public void GivenAlteryxRunningAt(string SUT_url)
        {
            alteryxurl = Environment.GetEnvironmentVariable(SUT_url);
        }

        [Given(@"I am logged in using ""(.*)"" and ""(.*)""")]
        public void GivenIAmLoggedInUsingAnd(string user, string password)
        {
            _sessionid = Obj.Authenticate(user, password).sessionId;
        }
        
        [When(@"I run the application """"(.*)"""" on the store ""(.*)"" and the category ""(.*)"" and """"(.*)""""")]
        public void WhenIRunTheApplicationOnTheStoreAndTheCategoryAnd(string app, string store, string category, string product)
        {
            //url + "/apps/gallery/?search=" + appName + "&limit=20&offset=0"
            //Search for App & Get AppId & userId 
            string response = Obj.SearchAppsGallery(app);
            var appresponse =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    response);
            int count = appresponse["recordCount"];
            if (count == 1)
            {
                _appid = appresponse["records"][0]["id"];
                _userid = appresponse["records"][0]["owner"]["id"];
                _appName = appresponse["records"][0]["primaryApplication"]["fileName"];
            }
            else
            {
                for (int i = 0; i <= count - 1; i++)
                {

                    _appActualName = appresponse["records"][i]["primaryApplication"]["metaInfo"]["name"];
                    if (_appActualName == app)
                    {
                        _appid = appresponse["records"][i]["id"];
                        _userid = appresponse["records"][i]["owner"]["id"];
                        _appName = appresponse["records"][i]["primaryApplication"]["fileName"];
                        break;
                    }
                }

            }

            jsString.appPackage.id = _appid;
            jsString.userId = _userid;
            jsString.appName = _appName;

            //url +"/apps/" + appPackageId + "/interface/
            //Get the app interface - not required
            string appinterface = Obj.GetAppInterface(_appid);
            dynamic interfaceresp = JsonConvert.DeserializeObject(appinterface);

            //Construct the payload to be posted.
            string cat = "|0|0";

            List<JsonPayload.Question> questionAnsls = new List<JsonPayload.Question>();
            questionAnsls.Add(new JsonPayload.Question("Select Categories or Products to Analyze", "["+"\""+cat+"\""+"]"));
            questionAnsls.Add(new JsonPayload.Question("Standard Date Range", "true"));
            questionAnsls.Add(new JsonPayload.Question("Calendar", "false"));
            questionAnsls.Add(new JsonPayload.Question("Select Date to Begin Analysis", "\"\""));
            jsString.questions.AddRange(questionAnsls);

            var loc = new List<JsonPayload.datac>();
            loc.Add(new JsonPayload.datac() { key = "001", value = "true" });
            string sloc = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(loc);

            var dt = new List<JsonPayload.datac>();
            dt.Add(new JsonPayload.datac() { key = "3", value = "true" });
            string dat = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(dt);

            for (int i = 0; i < 2; i++)
            {

                if (i == 0)
                {
                    JsonPayload.Question questionAns = new JsonPayload.Question();
                    questionAns.name = "Select the Store to Perform Analysis On";
                    questionAns.answer = sloc;
                    jsString.questions.Add(questionAns);
                }
                else if (i == 1)
                {
                    JsonPayload.Question questionAns = new JsonPayload.Question();
                    questionAns.name = "Date DropDown";
                    questionAns.answer = dat;
                    jsString.questions.Add(questionAns);
                }
            }
            jsString.jobName = "Job Name";
            // Make Call to run app

            var postData = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(jsString);
            string postdata = postData.ToString();
            string resjobqueue = Obj.QueueJob(postdata);

            var jobqueue =
                new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                    resjobqueue);
            jobid = jobqueue["id"];

            //Get the job status

            string status = "";
            while (status != "Completed")
            {
                string jobstatusresp = Obj.GetJobStatus(jobid);
                var statusresp =
                    new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string, dynamic>>(
                        jobstatusresp);
                status = statusresp["status"];
            }


        }
        
        [Then(@"I see the report generated """"(.*)""""")]
        public void ThenISeeTheReportGenerated(string result)
        {
            //url + "/apps/jobs/" + jobId + "/output/"
            string getmetadata = Obj.GetOutputMetadata(jobid);
            dynamic metadataresp = JsonConvert.DeserializeObject(getmetadata);

            // outputid = metadataresp[0]["id"];
            int count = metadataresp.Count;
            for (int j = 0; j <= count - 1; j++)
            {
                outputid = metadataresp[j]["id"];
            }

            string getjoboutput = Obj.GetJobOutput(jobid, outputid, "html");
            string htmlresponse = getjoboutput;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlresponse);
            string output = doc.DocumentNode.SelectSingleNode("//div[@class='DefaultText']").InnerHtml;
            StringAssert.Contains(result, output);
           
        }
    }
}

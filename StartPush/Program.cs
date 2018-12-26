using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StartPush
{
    class Program
    {
        public static UserCredential credential = null;
        public static GmailService service = null;

        public static string userId = "admin@awolr.com";

        static void Main(string[] args)
        {
            string[] Scopes = {"https://mail.google.com/",
            "https://www.googleapis.com/auth/pubsub"};


            string credPath = "token.json";
            try
            {
                Stream filestream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);



                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(filestream).Secrets,
                            Scopes,
                            "admin@awolr.com",
                            CancellationToken.None,
                            new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);


                service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "awolr.com"
                });

                watch();

            }
            catch (Exception e)
            {
                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("admin@awolr.com", "passWord321$"),
                    EnableSsl = true
                };



                client.Send("admin@awolr.com", "allanrodkin@gmail.com", "could not watch topic", System.DateTime.Now.ToLongTimeString());
            }
        }

        public static void watch()
        {
            try
            {
                WatchRequest body = new WatchRequest()
                {
                    TopicName = "projects/awolr-213414/topics/awolr",
                    LabelIds = new[] { "INBOX" }
                };
                string userId = "admin@awolr.com";
                UsersResource.WatchRequest watchRequest = service.Users.Watch(body, userId);
                WatchResponse test = watchRequest.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("cannot initiate watch request " + e);
            }
        }

        public static void stop()
        {
            try
            {
                UsersResource.StopRequest stopRequest = service.Users.Stop(userId);
                string test = stopRequest.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("cannot initiate stop request " + e);
            }
        }
    }
}

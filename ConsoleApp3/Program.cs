using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using RegistrationPractice.Classes;
using RegistrationPractice.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;


namespace ConsoleApp3
{
    class Program
    {
        public static GmailService service = null;

        public static UserCredential credential = null;

        public static string userId = "admin@awolr.com";


        private static LoggerWrapper loggerwrapper = new LoggerWrapper();

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

                //sendemail();
                //watch();
                //stop();
                history();
                //test();

            }
            catch (Exception e)
            {
                loggerwrapper.PickAndExecuteLogging("cannot initialize oauth" + e.ToString());
            }
        }

        public static void test()
        {
            var historyid = "12254";
            ulong longhistoryid = Convert.ToUInt64(historyid);

            var result = (long)longhistoryid;
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
                loggerwrapper.PickAndExecuteLogging("cannot initiate watch request " + e.ToString());
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
                loggerwrapper.PickAndExecuteLogging("cannot initiate stop request " + e.ToString());
            }
        }

        public static Message GetMessage(GmailService service, String userId, String messageId)
        {
            try
            {
                return service.Users.Messages.Get(userId, messageId).Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }

            return null;
        }

        // used to read database and send messages
        public static bool checkemaildb(EmailContext db, out Email outemail)
        {
            Email email = db.Emails.FirstOrDefault();
            if (email != null)
            {
                outemail = email;
                return true;
            }
            else
            {
                outemail = null;
                return false;
            }
        }

        public static bool checkhistoryiddb(EmailContext db, out HistoryID outhistoryid)
        {
            HistoryID historyid = db.HistoryIDs.LastOrDefault();
            if (historyid != null)
            {
                outhistoryid = historyid;
                return true;
            }
            else
            {
                outhistoryid = null;
                return false;
            }

        }

        public static void sendemail()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(10000);
                EmailContext db = new EmailContext();
                Email email = null;
                var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("admin@awolr.com", "passWord321$"),
                    EnableSsl = true
                };

                try
                {

                    while (checkemaildb(db, out email))
                    {
                        client.Send(email.fromaddress, email.toaddress, email.subject, email.emailbody);
                        loggerwrapper.PickAndExecuteLogging("send email record " + email.Id);

                    }
                }
                catch (Exception e)
                {

                }
            }
        }


        public static bool fullsync(out List<Message> messagelist, out ulong? historyid)
        {

            messagelist = null;
            List<Message> result = new List<Message>();
            List<Message> fullmessageobjects = new List<Message>();
            EmailContext db = new EmailContext();
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(userId);
            db.Database.ExecuteSqlCommand("TRUNCATE TABLE [HistoryID]");
            historyid = 0;
            do
            {
                try
                {
                    ListMessagesResponse response = request.Execute();
                    result.AddRange(response.Messages);
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return false;
                }
            } while (!String.IsNullOrEmpty(request.PageToken));

            if (result != null)
            {
                bool once = false;
                foreach (Message message in result)
                {
                    try
                    {
                        var fullmessageobject = service.Users.Messages.Get(userId, message.Id).Execute();
                        if (!once)
                        {
                            historyid = fullmessageobject.HistoryId;
                            once = true;
                        }

                        if (!fullmessageobject.LabelIds.Contains("SENT") && fullmessageobject.Payload.Headers[19].Value.Contains("AwolrID:"))
                        {

                            fullmessageobjects.Add(fullmessageobject);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occurred: " + e.Message);
                        return false;
                    }
                }
            }


            messagelist = fullmessageobjects;
            return true;
        }


        public static void history()
        {


            System.Threading.Thread thread = new System.Threading.Thread(sendemail);
            thread.Start();

            EmailContext db = new EmailContext();
            List<HistoryID> histories = db.HistoryIDs.ToList();
            List<Message> messagelist = new List<Message>();
            ulong? newesthistoryid;

            //email all history ids being processed

            bool synced = fullsync(out messagelist, out newesthistoryid);
            bool allrecordsmarkedasprocessed = true;

            if (synced)
            {
                foreach (Message message in messagelist)
                {
                    int index = message.Payload.Headers[19].Value.IndexOf("AwolrID:");
                    int length = message.Payload.Headers[19].Value.Length - 1;
                    string subjectline = message.Payload.Headers[19].Value.Substring((index + 8), length);
                    //write to database
                    // Decode
                    //var encodedString2 = "SGVsbG8gQmFzZTY0VXJsIGVuY29kaW5nIQ";
                    //var bytes2 = Base64Url.Decode(encodedString2);
                    //WriteLine(System.Text.Encoding.UTF8.GetString(bytes2)); // Hello Base64Url encoding!


                    ModifyMessageRequest mods = new ModifyMessageRequest();
                    List<String> addedlabels = new List<String> { "Label_5558979356135685998" };
                    List<String> removedlabels = new List<String> { };
                    mods.AddLabelIds = addedlabels;
                    mods.RemoveLabelIds = removedlabels;

                    try
                    {
                        service.Users.Messages.Modify(mods, userId, message.Id).Execute();
                    }
                    catch (Exception e)
                    {
                        allrecordsmarkedasprocessed = false;
                        loggerwrapper.PickAndExecuteLogging("message could not be marked as processed. messageid " + message.Id + e.ToString());
                    }
                }


            }
            //foreach (HistoryID historyid in histories)
            //{

            //    long id = historyid.History;
            //    try
            //    {

            //        ulong historyid_start_conversionresult;
            //        List<History> result = null;
            //        try
            //        {
            //            historyid_start_conversionresult = Convert.ToUInt64(id);
            //            result = ListHistory(service, "admin@awolr.com", historyid_start_conversionresult, messagelist);

            //        }
            //        catch (OverflowException)
            //        {
            //            Console.WriteLine("{0} is outside the range of the UInt64 type.", historyid);
            //        }
            //        if (result != null)
            //        {

            //            //loggerWrapper.PickAndExecuteLogging("Was able to retrieve history" + id.ToString());
            //        }
            //    }
            //    catch (Exception e)
            //    {

            //    }
            //}

        }

        public static byte[] Decode(string input)
        {
            var output = input;

            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding

            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break; // One pad char
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(input), "Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder

            return converted;
        }


        public static List<History> ListHistory(GmailService service, String userId, ulong startHistoryId, List<Message> messages)
        {
            List<History> result = new List<History>();
            UsersResource.HistoryResource.ListRequest request = service.Users.History.List(userId);
            request.HistoryTypes = UsersResource.HistoryResource.ListRequest.HistoryTypesEnum.MessageAdded;
            request.StartHistoryId = startHistoryId;
            request.LabelId = "INBOX";


            do
            {
                try
                {
                    ListHistoryResponse response = request.Execute();
                    if (response.History != null)
                    {
                        result.AddRange(response.History);
                        //foreach (History history in response.History)
                        //{
                        //    foreach (HistoryMessageAdded messagesadded in history.MessagesAdded)
                        //        messages.Add(messagesadded.Message);

                        //} 

                        foreach (HistoryMessageAdded messageadded in response.History.SelectMany(i => i.MessagesAdded))
                        {
                            if (!messageadded.Message.LabelIds.Contains("SENT"))
                            {
                                messages.Add(messageadded.Message);
                            }

                        }

                    }
                    request.PageToken = response.NextPageToken;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                }
            } while (!String.IsNullOrEmpty(request.PageToken));

            return result;
        }

    }










}

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
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        public static GmailService service = null;

        public static UserCredential credential = null;

        public static string userId = "admin@awolr.com";

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static LoggerWrapper loggerwrapper = new LoggerWrapper();


        static void ReadMail()
        {
            EmailContext db = new EmailContext();
            Email email = new Email();

            try
            {
                for (int x = 0; x < 250; x++)
                {
                    System.Threading.Thread.Sleep(2000);
                    email = db.Emails.FirstOrDefault();
                    Console.WriteLine("Read:" + email.Id.ToString());
                    logger.Debug("Read:" + email.Id.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not read email: " + e.ToString());
                logger.Debug("Could not read email: " + e.ToString());
            }



        }

        static void AddEmail()
        {
            EmailContext db = new EmailContext();
            Email email = new Email();
            email.emailbody = "123124234q45q345q";
            email.EmailRecipientsId = 1;
            email.fromaddress = "admin@awolr.com";
            email.IdItem = 114;
            email.ItemDescription = "asdfwef24234234";
            email.subject = "asdfasdf525asdfasdfasdfasdfasdf11123525";
            email.toaddress = "allanrodkin@gmail.com";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("admin@awolr.com", "passWord321$"),
                EnableSsl = true
            };

            try
            {
                for (int x = 0; x < 250; x++)
                {
                    System.Threading.Thread.Sleep(2000);
                    db.Emails.Add(email);
                    db.SaveChanges();
                    Console.WriteLine("Added email " + email.Id.ToString());
                    logger.Debug("Added email " + email.Id.ToString() + "\n\n");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not add email " + e.ToString());
                logger.Debug("Could not add email " + e.ToString() + "\n\n");
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
                logger.Debug("cannot initiate watch request " + e);
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
                logger.Debug("cannot initiate stop request " + e);
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
                logger.Debug("An error occurred retrieving message from api: " + e);
            }

            return null;
        }

        // used to read database and send messages
        public static bool checkemaildb(EmailContext db, out Email outemail)
        {
            System.Threading.Thread.Sleep(5000);
            Email email = db.Emails.FirstOrDefault();
            outemail = null;
            try
            {

                if (email != null)
                {
                    outemail = email;
                    return true;
                }
                else
                {

                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Debug("Error in checkmaildb:" + e + "------------------\n\n");
                return false;
            }
        }

        public static bool checkhistoryiddb(EmailContext db, out ulong? outhistoryid)
        {
            outhistoryid = 0;

            HistoryID historyid = db.HistoryIDs.FirstOrDefault();

            try
            {

                if (historyid != null)
                {
                    outhistoryid = (ulong)historyid.History;

                    return true;
                }
                else
                {
                    outhistoryid = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Debug("Error in checkmaildb:" + e + "-------------------\n\n");
                return false;
            }

        }

        public static void SendEmail()
        {
            while (true)
            {
                //System.Threading.Thread.Sleep(10000);
                EmailContext db = new EmailContext();
                Email email = null;
                bool success2;

                try
                {

                    while (checkemaildb(db, out email))
                    {
                        SendThroughGmail(db, email);

                    }
                }
                catch (Exception e)
                {
                    logger.Debug("failed to send email :" + e + "-------------------\n\n");
                }
            }
        }



        public static void SendThroughGmail(EmailContext db, Email email)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("admin@awolr.com", "passWord321$"),
                EnableSsl = true
            };



            client.Send(email.fromaddress, email.toaddress, email.subject, email.emailbody);
            //loggerwrapper.PickAndExecuteLogging("send email record " + email.Id);
            db.Emails.Remove(email);
            db.SaveChanges();
            Console.WriteLine("Deleted email " + email.Id);

        }

        public static bool fullsync(out List<Message> messagelist, out ulong? historyid)
        {

            historyid = 666;
            messagelist = null;
            List<Message> result = new List<Message>();
            List<Message> fullmessageobjects = new List<Message>();
            EmailContext db = new EmailContext();
            UsersResource.MessagesResource.ListRequest request = service.Users.Messages.List(userId);

            try
            {
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE [HistoryID]");

            }
            catch (Exception e)
            {
                logger.Debug("Could not truncate HistoryID table! " + e.ToString() + "--------------\n\n");
                return false;
            }

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
                    logger.Debug("An error occurred in full sync: " + e.ToString() + "--------------\n\n");
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

                        Google.Apis.Gmail.v1.Data.MessagePartHeader subjectheader = fullmessageobject.Payload.Headers.Where(h => h.Name == "Subject").FirstOrDefault();
                        fullmessageobject.Payload.Headers[0] = subjectheader;
                        if (!fullmessageobject.LabelIds.Contains("SENT") && !fullmessageobject.LabelIds.Contains("Label_5558979356135685998") && subjectheader.Value.Contains("AwolrID:"))
                        {

                            fullmessageobjects.Add(fullmessageobject);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Debug("An error occurred trying to get message from api or assign to internal list: " + e.ToString() + "-------------------\n\n");
                        return false;
                    }
                }
            }


            messagelist = fullmessageobjects;
            return true;
        }


        public static void history()
        {

            EmailContext db = new EmailContext();
            List<HistoryID> histories = db.HistoryIDs.ToList();
            List<Message> messagelist = new List<Message>();
            ulong? newesthistoryid;

            //email all history ids being processed

            while (true)
            {
                System.Threading.Thread.Sleep(2000);

                while (checkhistoryiddb(db, out newesthistoryid))
                {

                    bool synced = fullsync(out messagelist, out newesthistoryid);
                    bool allrecordsmarkedasprocessed = true;

                    if (synced && messagelist.Count > 0)
                    {
                        foreach (Message message in messagelist)
                        {
                            string subjectline = message.Payload.Headers[0].Value;
                            int index = subjectline.IndexOf("AwolrID:");
                            string awolrid = message.Payload.Headers[0].Value.Substring(index + 8);
                            string rebuild = subjectline.Substring(0, index - 1);
                            //write to database
                            // Decode
                            string body = " ";
                            var encodedString2 = message.Payload.Parts[0].Body.Data;
                            if (encodedString2 != null)
                            {
                                var bytes2 = Decode(encodedString2);
                                body = System.Text.Encoding.UTF8.GetString(bytes2); // Hello Base64Url encoding!
                            }

                            string to = null;
                            try
                            {

                                EmailRecipients emailRecipients =
                                db.EmailRecipients.Where(er => er.bidfakeemailaddress.Contains(awolrid) || er.pidfakeemailaddress.Contains(awolrid)).FirstOrDefault();

                                if (emailRecipients.bidfakeemailaddress == awolrid)
                                {
                                    subjectline = rebuild + " AwolrID:" + emailRecipients.pidfakeemailaddress;
                                    to = emailRecipients.bidrealemailaddress;
                                }
                                else
                                {
                                    subjectline = rebuild + " AwolrID:" + emailRecipients.bidfakeemailaddress;
                                    to = emailRecipients.pidrealemailaddress;
                                }

                                try
                                {
                                    Email email = new Email();
                                    email.emailbody = body;
                                    email.EmailRecipientsId = emailRecipients.Id;
                                    email.fromaddress = "admin@awolr.com";
                                    email.toaddress = to;
                                    email.IdItem = emailRecipients.IdItem;
                                    email.subject = subjectline;
                                    email.ItemDescription = subjectline;
                                    db.Emails.Add(email);
                                    db.SaveChanges();
                                    Console.WriteLine("New Email Record Generated For Email " + email.Id);

                                }
                                catch (Exception e)
                                {
                                    logger.Debug("Could not create relay response email. Returning method" + e.ToString() + "--------------\n\n");
                                    return;
                                }
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

                                    throw new Exception("message could not be marked as processed.messageid " + message.Id + e.ToString());
                                }

                            }
                            catch (Exception e)
                            {
                                logger.Debug("message could not be marked as processed. messageid: " + message.Id + ":" + e.ToString() + "--------------\n\n");
                            }


                        }
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

                //watch();

                //System.Threading.Thread thread = new System.Threading.Thread(AddEmail);
                //thread.Start();



                //System.Threading.Thread thread3 = new System.Threading.Thread(SendEmail);
                //thread3.Start();
                //sendemail();

                //System.Threading.Thread thread4 = new System.Threading.Thread(SendGmail);
                //thread4.Start();

                //stop();

                System.Threading.Thread thread2 = new System.Threading.Thread(SendEmail);
                thread2.Start();

                System.Threading.Thread thread9 = new System.Threading.Thread(history);
                thread9.Start();
                //test();

            }
            catch (Exception e)
            {
                logger.Debug("cannot initialize oauth" + e + "-----------------\n\n");
            }
        }

        public static void SendGmail()
        {
            var clients = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("allanrodkin@gmail.com", "V1l0l5a4ayr"),
                EnableSsl = true
            };


            while (true)
            {
                System.Threading.Thread.Sleep(5000);
                try
                {
                    clients.Send("allanrodkin@gmail.com", "admin@awolr.com", "Message from awolr.com Lost Item 114 | Title: cell phone | Category:mobile device/tablets Description: brand new with pink case from bell: || AwolrID:0pz0zvw5.fgr", "testing");
                }
                catch (Exception e)
                {

                }
            }

        }

    }










}

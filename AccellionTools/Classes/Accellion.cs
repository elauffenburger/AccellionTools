using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Http;
using System.Web;
using System.Security;
using System.IO;
using System.Security.Cryptography;
using HtmlAgilityPack;

using AccellionTools.Exceptions;
using AccellionTools.Helpers;

namespace AccellionTools
{
    public static class Accellion
    {
        public static string ID;
        public static string Index;
        public static string Value;
        public static string handlePrefix;

        private static bool Configured = false;

        public static void Initialize(AccellionInitializationOptions options)
        {
            try
            {
                ID = options.ID;
                Index = options.Index;
                Value = options.Value;
                handlePrefix = options.handlePrefix;
            }
            catch (Exception)
            {
                Configured = false;
                throw new AccellionInitializationException("Make sure you've assigned values for all options");
            }

            Configured = true;
        }

        [Obsolete("Use Initialize with AccellionInitializationOptions instead")]
        public static void Initialize(string id, string index, string value, string handle_prefix) {
            ID = id;
            Index = index;
            Value = value;
            handlePrefix = handle_prefix;
            Configured = true;
        }

        private static string CreateCS(string index, string value, string expiry, string email)
        {
            byte[] bytes = SHA1.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(index + expiry + email));
            string CS1 = BytesToString(bytes);
            return string.Format("{0}-{1}-{2}", index, expiry, BytesToString(SHA1.Create().ComputeHash(System.Text.Encoding.ASCII.GetBytes(value + expiry + email))));

        }

        public static string GetFileName(string filename)
        {
            string[] filenameparts = filename.Split('.');
            string filenameprefix = "";
            for (int i = 0; i < filenameparts.Length - 1; i++)
            {
                if (i == filenameparts.Length - 2)
                {
                    filenameprefix += filenameparts[i];
                    break;
                }
                filenameprefix += filenameparts[i] + ".";
            }
            return filenameprefix + "_" + DateTime.UtcNow.ToFileTimeUtc() + "." + filenameparts[filenameparts.Length - 1];
        }

        private static string BytesToString(byte[] bytes)
        {
            return String.Concat(Array.ConvertAll(bytes, b => b.ToString("X2"))).ToLower();
        }

        private static string HashBrowns(string str)
        {
            MD5 hasher = MD5.Create();
            byte[] hashedBytes = hasher.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
            return BytesToString(hashedBytes);
        }

        private static string createAccellionToken(string api, string method, string expiry, string optParams, string Value, string ID, string Index)
        {
            string H1 = HashBrowns(string.Format("{0}{1}{2}@*{3}", api, method, expiry, optParams));
            string H2 = HashBrowns(string.Format("{0}{1}{2}", Value, ID, H1));
            return string.Format("x{0}@{1}@{2}@*{3}", Index, H2, expiry, optParams);
        }

        public static void sendAccellion(string userid, string recips, byte[] fileBytes, string filename)
        {
            if (!Configured)
            {
                throw new AccellionConfigurationException();
            }

            string mungedFileName = userid + "/" + GetFileName(filename);
            putFile(fileBytes, mungedFileName);
            sendFile(recips, mungedFileName);
        }

        private static void putFile(byte[] fileBytes, string mungedFileName)
        {
            string api = "put";

            Dictionary<string, string> paramsDictionary = new Dictionary<string, string>();
            paramsDictionary.Add("file_handle", handlePrefix + mungedFileName);
            paramsDictionary.Add("overwrite", "0");

            string optParams = generateOptionalParams(paramsDictionary);
            string hashToken = createAccellionToken(api, "", getExpiry(), optParams, Value, ID, Index);

            string splitFile = mungedFileName.Split('/')[mungedFileName.Split('/').Length - 1];
            HttpClient client = new HttpClient();
            MultipartFormDataContent formData = new MultipartFormDataContent();
            formData.Add(new ByteArrayContent(fileBytes), "file", splitFile);
            formData.ElementAt(0).Headers.Add("Content-Type", MimeMapping.GetMimeMapping(splitFile));
            formData.Add(new StringContent(hashToken), "token");
            formData.Add(new StringContent(paramsDictionary["file_handle"].ToString()), "file_handle");

            postAsync(client, formData, api);
        }

        private static void sendFile(string recips, string filename)
        {
            string api = "send";

            Dictionary<string, string> paramsDictionary = new Dictionary<string, string>();
            paramsDictionary.Add("file1", handlePrefix + filename);
            paramsDictionary.Add("subject", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("eSign - You've Received a Secure Document")));
            paramsDictionary.Add("message", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("Thank you for using eSign!")));
            paramsDictionary.Add("link_validity", "45");
            paramsDictionary.Add("recipients", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(recips)));

            string optParams = generateOptionalParams(paramsDictionary);
            string hashToken = createAccellionToken(api, "", getExpiry(), optParams, Value, ID, Index);

            HttpClient client = new HttpClient();
            MultipartFormDataContent formData = new MultipartFormDataContent();
            formData.Add(new StringContent(hashToken), "token");
            formData.Add(new StringContent(paramsDictionary["file1"].ToString()), "file1");
            formData.Add(new StringContent(paramsDictionary["subject"].ToString()), "subject");
            formData.Add(new StringContent(paramsDictionary["link_validity"].ToString()), "link_validity");
            formData.Add(new StringContent(paramsDictionary["recipients"].ToString()), "recipients");

            postAsync(client, formData, api);
        }

        public static List<AccellionFileRequest> GetFiles(string emailid, bool getBytes = false)
        {
            if (!Configured)
            {
                throw new AccellionConfigurationException();
            }

            string api = "find";
            string method = "inbox";
            string emailID = emailid;

            Dictionary<string, string> paramsDictionary = new Dictionary<string, string>();
            paramsDictionary.Add("method", method);
            paramsDictionary.Add("cs", CreateCS(Index, Value, (DateTime.UtcNow - (new DateTime(1970, 1, 1, 0, 0, 0, 0))).TotalSeconds.ToString().Split('.')[0], emailID));

            string optParams = generateOptionalParams(paramsDictionary);
            string hashToken = createAccellionToken(api, method, getExpiry(), optParams, Value, ID, Index);

            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = new CookieContainer()
            };

            HttpClient client = new HttpClient(handler);

            List<KeyValuePair<string, string>> reqParams = new List<KeyValuePair<string, string>>();
            reqParams.Add(new KeyValuePair<string, string>("token", hashToken));
            foreach (var d in paramsDictionary)
            {
                reqParams.Add(new KeyValuePair<string, string>(d.Key, d.Value));
            }

            FormUrlEncodedContent formURLData = new FormUrlEncodedContent(reqParams);

            string fileList = postAsync(client, formURLData, api);
            List<AccellionFileRequest> AccellionFiles = new List<AccellionFileRequest>();
            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(fileList.Replace("\n", ""));

            HtmlNodeCollection packages = doc.DocumentNode.SelectNodes("//var[@name='packages']/struct/var");
            foreach (var pack in packages)
            {
                AccellionFileRequest tempRequest = new AccellionFileRequest()
                {
                    Url = pack.SelectSingleNode("./struct/var[@name='package_files']/array/struct/var[@name='url']").InnerText.Trim(),
                    FileName = pack.SelectSingleNode(".").Attributes["name"].Value
                };

                if (getBytes)
                {
                    HttpWebRequest request = HttpWebRequest.CreateHttp(tempRequest.Url);
                    
                    using(MemoryStream memStream = new MemoryStream()) {
                        memStream.Seek(0, SeekOrigin.Begin);
                        request.GetResponse().GetResponseStream().CopyTo(memStream);
                        tempRequest.Bytes = memStream.GetBuffer();
                    }
                }

                AccellionFiles.Add(tempRequest);
            }
      
            try
            {
                handler.CookieContainer.Add(new Cookie(string.Format("a{0}c1s1", ID), string.Format("user&{0}&cs&{1}", emailID, reqParams.Find(p => p.Key.Equals("cs")).Value))
                {
                    Domain = ".pepperdine.edu"
                });

                HttpResponseMessage response = new HttpResponseMessage();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return AccellionFiles;
        }

        private static string getExpiry()
        {
            return DateTime.Today.AddDays(1).ToString("yyyy-MM-dd 23:59:59");
        }

        private static string generateOptionalParams(Dictionary<string, string> paramsDictionary)
        {
            StringBuilder paramsBuilder = new System.Text.StringBuilder();
            string optParams = "";
            foreach (var k in paramsDictionary)
            {
                paramsBuilder.Append(string.Format("@{0}={1}", k.Key, k.Value));
            }
            optParams = paramsBuilder.ToString();
            return optParams;
        }

        private static string postAsync(HttpClient client, object formData, string api)
        {
            HttpResponseMessage response = null;
            try
            {
                Type requestType = formData.GetType();

                // Determine RequestType
                if (requestType.Equals(typeof(FormUrlEncodedContent)))
                {
                    response = client.PostAsync(string.Format(@"https://attachments1.pepperdine.edu/seos/1000/{0}.api", api), (FormUrlEncodedContent)formData).Result;
                }
                else if (requestType.Equals(typeof(MultipartFormDataContent)))
                {
                    response = client.PostAsync(string.Format(@"https://attachments1.pepperdine.edu/seos/1000/{0}.api", api), (MultipartFormDataContent)formData).Result;
                }
                else
                {
                    throw new InvalidRequestTypeException(string.Format(@"Type ""{0}"" is not a supported Content-Type object", requestType.ToString()));
                }

                Stream responseStream = response.Content.ReadAsStreamAsync().Result;
                StreamReader reader = new StreamReader(responseStream);
                return reader.ReadToEnd();
            }
            catch (InvalidRequestTypeException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

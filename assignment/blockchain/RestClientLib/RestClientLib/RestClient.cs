using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace RestClientLib
{
    public class RestClient
    {
        System.Net.ICredentials _credentials;

        public RestClient()
        {
            _credentials = System.Net.CredentialCache.DefaultCredentials;
        }

        public RestClient(System.Net.ICredentials credentials)
        {
            _credentials = credentials;
        }

        //Calls get, accepts a url and returns a json string
        public string Get(string url)
        {
            string json = "";
            using (var client = new WebClient())
            {
                client.Credentials = _credentials;
                json = client.DownloadString(url);
            }
            return json;
        }

        //gets binary data from the url
        public byte[] GetBinary(string url)
        {
            byte[] result;
            using (var client = new WebClient())
            {
                client.Credentials = _credentials;
                result = client.DownloadData(url);
            }
            return result;
        }

        //Call REST API Post/ accepts a json string, returns a json string
        public string Post(string url, string json)
        {
            string result = "";
            using (var client = new WebClient())
            {
                client.Credentials = _credentials;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                result = client.UploadString(url, "POST", json);
            }
            return result;
        }

    }

}

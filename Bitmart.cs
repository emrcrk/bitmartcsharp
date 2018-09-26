 
using Jayrock.Json;
using Jayrock.Json.Conversion; 
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BitmartCSharp
{
    class BitmartWallet
    {
        [Newtonsoft.Json.JsonProperty("available")]
        public string Available { get; set; }
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }
        [Newtonsoft.Json.JsonProperty("frozen")]
        public string Frozen { get; set; }
        [Newtonsoft.Json.JsonProperty("id")]
        public string Id { get; set; }
    }

    class BitmartTicker
    {
        [Newtonsoft.Json.JsonProperty("volume")]
        public string  Volume { get; set; }
        [Newtonsoft.Json.JsonProperty("base_volume")]
        public string Base_Volume { get; set; }
        [Newtonsoft.Json.JsonProperty("highest_price")]
        public string Highest_Price { get; set; }
        [Newtonsoft.Json.JsonProperty("lowest_price")]
        public string Lowest_Price { get; set; }
        [Newtonsoft.Json.JsonProperty("current_price")]
        public string Current_Price { get; set; }
        [Newtonsoft.Json.JsonProperty("ask_1")]
        public string Ask_1 { get; set; }
        [Newtonsoft.Json.JsonProperty("ask_1_amount")]
        public string Ask_1_Amount { get; set; }
        [Newtonsoft.Json.JsonProperty("bid_1")]
        public string Bid_1 { get; set; }
        [Newtonsoft.Json.JsonProperty("bid_1_amount")]
        public string Bid_1_Amount { get; set; }
        [Newtonsoft.Json.JsonProperty("fluctuation")]
        public string Fluctuation { get; set; }
        [Newtonsoft.Json.JsonProperty("url")]
        public string Url { get; set; }
    }

    class Bitmart
    {
        private string CreateSignature(string message)
        { 
            return ByteArrayToString(SignHMACSHA256(API_PRIVATEKEY, StringToByteArray(message))) ;
        }

        private   byte[] SignHMACSHA256(string key, byte[] data)
        {
            var hashMaker = new HMACSHA256(Encoding.ASCII.GetBytes(key));
            return hashMaker.ComputeHash(data);
        }

        private   byte[] StringToByteArray(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        private   string ByteArrayToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }



        public const string ROOT_URI = "https://openapi.bitmart.com/";
        string API_KEY;
        string API_PRIVATEKEY;

        public Bitmart(string apiKey,string privateKey)
        {
            API_KEY = apiKey;
            API_PRIVATEKEY = privateKey;


        }

        public string GetBearerToken()
        { 
            RestClient restClient = new RestClient(ROOT_URI);
            RestRequest restRequest = new RestRequest("v2/authentication",Method.POST);
             
            restRequest.RequestFormat = DataFormat.Json; 
            restRequest.AddParameter("grant_type", "client_credentials");
            restRequest.AddParameter("client_id", API_KEY); 
            restRequest.AddParameter("client_secret", API_PRIVATEKEY);

            IRestResponse response = restClient.Execute(restRequest);
            var content =response.Content; 
            string token = "";
            var jsonData =(JsonObject) JsonConvert.Import(content);
            if (jsonData.Contains("access_token"))
            {
                 token = jsonData["access_token"].ToString();
            }
            return token;
        }
        
        public List<BitmartWallet> GetWallet()
        {

            List<BitmartWallet> wallets = new List<BitmartWallet>();

            RestClient restClient = new RestClient(ROOT_URI);
            RestRequest restRequest = new RestRequest("v2/wallet", Method.GET);
            string Token = GetBearerToken();
            if (Token =="")
            {
                return wallets;
            }
            restRequest.RequestFormat = DataFormat.Json;
            restRequest.AddHeader("X-BM-TIMESTAMP", GetNonce());
            restRequest.AddHeader("X-BM-AUTHORIZATION", "Bearer " + Token);
            IRestResponse response = restClient.Execute(restRequest);
            var content = response.Content; 
            var jsonData = (JsonArray)JsonConvert.Import(content);
            foreach (var item in jsonData)
            {
                BitmartWallet wallet = JsonObjectSerialize.ObjectSerialize<BitmartWallet>(item.ToString());
                wallets.Add(wallet);
            }
            return wallets;
        }

        public string GetNonce()
        {
            long timestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            return timestamp.ToString();
        }

        public BitmartTicker GetTicker(string symbol)
        {
            BitmartTicker retVal =new BitmartTicker();

            RestClient restClient = new RestClient(ROOT_URI);
            RestRequest restRequest = new RestRequest("v2/ticker?symbol="+ symbol, Method.GET);
            IRestResponse response = restClient.Execute(restRequest);
            var content = response.Content;
            
            if (content!="")
            {
                var jsonData = (JsonObject)JsonConvert.Import(content);

                retVal = JsonObjectSerialize.ObjectSerialize<BitmartTicker>(jsonData.ToString());
            }
           
            return retVal;
        }

        public string PostOrder(string symbol,string amount,string price,string side,out bool success)
        {
            success = false;
            RestClient restClient = new RestClient(ROOT_URI);
            RestRequest restRequest = new RestRequest("v2/orders", Method.POST);

            string Token = GetBearerToken();
            if (Token == "")
            { 
                return "-";
            }

            string message =String.Format("amount={0}&price={1}&side={2}&symbol={3}", amount, price, side, symbol);
            string sing = CreateSignature(message);
            string Nonce = GetNonce();
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("X-BM-TIMESTAMP", Nonce);
            restRequest.AddHeader("X-BM-AUTHORIZATION", "Bearer " + Token); 
            restRequest.AddHeader("X-BM-SIGNATURE", sing);

            JsonObject body = new JsonObject();
            body.Add("symbol", symbol);
            body.Add("amount", amount);
            body.Add("price", price);
            body.Add("side", side);

            restRequest.AddJsonBody(body);
             

            IRestResponse response = restClient.Execute(restRequest);
            var content = response.Content;
            string result = "";
            var jsonData = (JsonObject)JsonConvert.Import(content);
            if (jsonData.Contains("entrust_id"))
            {
                success = true;
                result = jsonData["entrust_id"].ToString();
            }
            if (jsonData.Contains("message"))
            { 
                result = jsonData["message"].ToString();
            }
            return result;
        }
    }
}

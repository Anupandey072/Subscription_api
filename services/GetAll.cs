using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Pkcs;

namespace subscription_api.services
{
    public class GetAll
    {
        private readonly dbServiceMongo _ds;
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();

        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        public GetAll()
        {

            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);
        }
      public async Task<responseData> Dashboard(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "Selected Data";

    try
    {
        var pipeline = new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "_user_id", ObjectId.Parse(req.addInfo["_user_id"].ToString()) }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "m_Subscription_Users" },
                { "localField", "_user_id" },
                { "foreignField", "_id" },
                { "as", "joinedData" }
            }),
            new BsonDocument("$unwind", "$joinedData"),
            new BsonDocument("$project", new BsonDocument
            {
            //    {"id","$_id"},
                  { "_name", "$joinedData._name" },
                  { "_mobile_no", "$joinedData._mobile_no" },
                  { "_email_id", "$joinedData._email_id" },
                  { "_pin_code", "$joinedData._pin_code" },
                  { "_address", "$joinedData._address" },
                  { "document", "$document" },
            })
        };

        mongoRequest mRequest = new mongoRequest();
        mongoResponse mResponse = new mongoResponse();
        mRequest.newRequestStatement(4, "m_Subscription_user_Doc", null, null, null, pipeline);
        mResponse = await _ds.executeStatements(mRequest, false);
       
        var result = mResponse._resStatements[0]._selectedResults;
        if (result != null && result.Any()) {

        var certificateData = new Dictionary<string, object>();

        var list = new List<Dictionary<string, object>>();
        
// anu
 foreach (var results in result)
            {
                var dict = new Dictionary<string, object>
                {
                    { "_name", results["_name"].ToString() },
                        { "_mobile_no", results["_mobile_no"].ToString() },
                        { "_email_id", results["_email_id"].ToString() },
                        { "_pin_code", results["_pin_code"].ToString() },
                        { "_address", results["_address"].ToString() },

 { "document", new List<Dictionary<string, object>>() }
    };
    var documents = results["document"].AsBsonArray;
    foreach (var doc in documents)
    {
        var certDict = new Dictionary<string, object>
        {
            { "certificateType", doc["certificateType"].ToString() },
            { "certificateBase64", new Dictionary<string, object>
                {
                    { "name", doc["certificateBase64"]["name"].ToString() },
                    { "size", doc["certificateBase64"]["size"].ToInt64() },
                    { "type", doc["certificateBase64"]["type"].ToString() },
                    { "base64Content", doc["certificateBase64"]["base64Content"].ToString() }
                }
            },
            { "certificateNumber", doc["certificateNumber"].ToString() }
        };

        ((List<Dictionary<string, object>>)dict["document"]).Add(certDict);
    }
            
list.Add(dict);      
                };
            resData.eventID = req.eventID;
            resData.rData["rData"] = list;
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "Data Not Found";
        }
    }
    catch (Exception ex)
    {
        resData.rStatus = 199;
        resData.rData["rCode"] = 199;
        resData.rData["rMessage"] = "An error occurred: " + ex.Message;
        Console.WriteLine("Error in GetOrderHistory: " + ex.ToString());
    }

    return resData;
}




    }
}
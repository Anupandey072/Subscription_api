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
    public class updateUser
    {
        private readonly dbServiceMongo _ds;
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();
        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        public updateUser()
        {
            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);
        }
public async Task<responseData> updateUsers(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "Updated Successfully";
    mongoResponse mResponse = new mongoResponse();
    try
    {
        BsonDocument filters = new BsonDocument
        {
            {"_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
        };
 BsonDocument filters1 = new BsonDocument
        {
            { "_id", ObjectId.Parse(req.addInfo["_id"].ToString()) },
        };

            mongoRequest mRequest = new mongoRequest();
            mRequest.newRequestStatement(0, "m_Subscription_Users", filters, null, null, null);
            mResponse = await _ds.executeStatements(mRequest, false);
            var res = mResponse._resStatements[0]._selectedResults;
            if (res.Count() > 0)
            {
                BsonDocument update = new BsonDocument
            {
                {"$set", new BsonDocument
                    {
                        {"_name", req.addInfo["_name"].ToString()},
                        {"_mobile_no",req.addInfo["_mobile_no"].ToString()},
                        {"_email_id",req.addInfo["_email_id"].ToString()},
                        {"_pin_code",req.addInfo["_pin_code"].ToString()},
                        {"_address",req.addInfo["_address"].ToString()},
                         {"Status","1"},
                    }
                }
            };
                 mongoResponse mResponse1 = new mongoResponse();
                 mongoRequest mRequest1 = new mongoRequest();
                 mRequest1.newRequestStatement(3, "m_Subscription_Users", filters, null, update, null);
         mResponse1 = await _ds.executeStatements(mRequest1, false);
        if (mResponse1 != null && mResponse1._resStatements != null && mResponse1._resStatements.Count > 0)
        {
            resData.eventID = req.eventID;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Update successful";
       
            }
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "Update failed"; // Update message if the update failed
        }  
    }
            
    catch (Exception ex)
    {
        resData.rStatus = 199;
        resData.rData["rCode"] = 199;
        resData.rData["rMessage"] = "An error occurred: " + ex.Message;
        Console.WriteLine("Error in updateUsers: " + ex.ToString());
    }
    return resData;
}

  public async Task<responseData> updateUserDoc(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "Documents Updated Successfully";
    
    try
    {
        BsonDocument filters = new BsonDocument
        {
            { "_id", ObjectId.Parse(req.addInfo["_id"].ToString()) },
        };
        
        BsonDocument update = new BsonDocument();
        
        if (req.addInfo.ContainsKey("document"))
        {
            var existingCerts = JArray.Parse(req.addInfo["document"].ToString());
            var mergedCerts = new BsonArray();
            foreach (var cert in existingCerts)
            {
                cert["doc_id"]=ObjectId.GenerateNewId().ToString();
                mergedCerts.Add(BsonDocument.Parse(cert.ToString()));
            }
            update["$set"] = new BsonDocument("document", mergedCerts);
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "No documents to update";
        }
        
        mongoResponse mResponse = new mongoResponse();
        mongoRequest mRequest = new mongoRequest();
        mRequest.newRequestStatement(3, "m_Subscription_user_Doc", filters, null, update, null);
        mResponse = await _ds.executeStatements(mRequest, false);
        
        if (mResponse != null && mResponse._resStatements != null && mResponse._resStatements.Count > 0)
        {
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Documents Update successful";
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "Documents Update failed"; // Update message if the update failed
        }
    }
    
    catch (Exception ex)
    {
        resData.rStatus = 199;
        resData.rData["rCode"] = 199;
        resData.rData["rMessage"] = "An error occurred: " + ex.Message;
        Console.WriteLine("Error in updateUsers: " + ex.ToString());
    }
    return resData;
}

public async Task<responseData> updatePassword(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "Password Updated Successfully";
    mongoResponse mResponse = new mongoResponse();
    try
    {
        BsonDocument filters = new BsonDocument
        {
            {"_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
            {"_password",req.addInfo["OldPassword"].ToString()}
        };
 BsonDocument filters1 = new BsonDocument
        {
             {"_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
            {"_password",req.addInfo["NewPassword"].ToString()}
        };

            mongoRequest mRequest = new mongoRequest();
            mRequest.newRequestStatement(0, "m_Subscription_Users", filters, null, null, null);
            mResponse = await _ds.executeStatements(mRequest, false);
            var res = mResponse._resStatements[0]._selectedResults;
            if (res.Count() > 0)
            {
                BsonDocument update = new BsonDocument
            {
                {"$set", new BsonDocument
                    {
                        {"_password", req.addInfo["NewPassword"].ToString()},
                        
                        
                    }
                }
            };
                 mongoResponse mResponse1 = new mongoResponse();
                 mongoRequest mRequest1 = new mongoRequest();
                 mRequest1.newRequestStatement(3, "m_Subscription_Users", filters, null, update, null);
         mResponse1 = await _ds.executeStatements(mRequest1, false);
        if (mResponse1 != null && mResponse1._resStatements != null && mResponse1._resStatements.Count > 0)
        {
            resData.eventID = req.eventID;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Password Updated successful";
       
            }
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "Update failed....."; // Update message if the update failed
        }  
    }
            
    catch (Exception ex)
    {
        resData.rStatus = 199;
        resData.rData["rCode"] = 199;
        resData.rData["rMessage"] = "An error occurred: " + ex.Message;
        Console.WriteLine("Error in updateUsers: " + ex.ToString());
    }
    return resData;
}
    }
}
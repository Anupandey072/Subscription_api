using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace subscription_api.services
{
    public class subscription
    {
        private readonly dbServiceMongo _ds;
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();

        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        public subscription()
        {

            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);
        }

        public async Task<responseData> addDetails(requestData req, string objectId)
        {
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "User Subscribed";

            mongoResponse mResponse = new mongoResponse();
            try
            {
                DateTime currentDate = DateTime.Now;
                DateTime fromDate;
                DateTime tillDate;

                string planType = req.addInfo["plan_type"].ToString();

                if (planType == "Monthly")
                {
                    fromDate = currentDate;
                    tillDate = currentDate.AddMonths(1);
                }
                else if (planType == "Yearly")
                {
                    fromDate = currentDate;
                    tillDate = currentDate.AddYears(1);
                }
                else
                {
                    throw new Exception("Invalid plan specified.");
                }

                // Parse quantity as an integer
                if (!int.TryParse(req.addInfo["quantity"].ToString(), out int quantity))
                {
                    throw new Exception("Invalid quantity specified.");
                }

                var documents = new[]
                {
            new BsonDocument
            {
                { "_user_id", ObjectId.Parse(objectId).ToString() },
                {"plan_type", planType},
                {"amount", req.addInfo["amount"].ToString()},
                {"planresult", req.addInfo["planresult"].ToString()},
                {"plan", req.addInfo["plan"].ToString()},
                {"icon", req.addInfo["icon"].ToString()},
                {"quantity", quantity},
                {"plandetail", req.addInfo["plandetail"].ToString()},
                {"from_date", fromDate.ToString("dd-MM-yyyy")},
                {"till_date", tillDate.ToString("dd-MM-yyyy")},
                {"status","1"}
            }
        };

                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(1, "m_Subscription", null, null, null, documents);
                mResponse = await _ds.executeStatements(mRequest, true);
                mResponse.session.CommitTransaction();

                // Update _subscription_status in m_Subscription_Users
                BsonDocument filter = new BsonDocument { { "_id", ObjectId.Parse(objectId) } };
                BsonDocument update = new BsonDocument("$set", new BsonDocument { { "_subscription_status", "1" } });

                mongoRequest updateRequest = new mongoRequest();
                updateRequest.newRequestStatement(3, "m_Subscription_Users", filter, null, update, null);
                mResponse = await _ds.executeStatements(updateRequest, false);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message.ToString());
                if (mResponse.session is not null)
                    mResponse.session.AbortTransaction();
                resData.rStatus = 199;
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Error in Inserting Data. Please try again !!!  " + ex.Message.ToString();
            }
            return resData;
        }      

        public async Task<responseData> GetSubscriptionPlan(requestData req)
        {
            mongoResponse mResponse = new mongoResponse();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Selected Data";

            try
            {
                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(0, "m_Subscription_Plans", null, null, null, null);
                mResponse = await _ds.executeStatements(mRequest, false);
                var res = mResponse._resStatements[0]._selectedResults;
                if (res.Count() > 0)
                {
                    var list = new List<object>();

                    if (res != null && res.Count() > 0)
                    {
                        for (var i = 0; i < res.Count(); i++)
                        {
                            var myDict = new Dictionary<string, object>();
                            foreach (var field in res[i].Names)
                            {
                                myDict[field] = res[i][field].ToString();
                            }
                            list.Add(myDict);
                        }
                    }
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
                resData.rData["rMessage"] = "REMOVE THIS ERROR IN PRODUCTION !!!  " + ex.Message.ToString();
            }
            return resData;
        }
     public async Task<responseData> PurchaseSubscription(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "User ...";

    mongoResponse mResponse = new mongoResponse();
    try
    {
        var documents = new[]
        {
            new BsonDocument
            {
                { "_user_id", ObjectId.Parse(req.addInfo["_user_id"].ToString())},
                {"_start_date", req.addInfo["_start_date"].ToString()},
                {"_end_date", req.addInfo["_end_date"].ToString()},
                {"_plan_id", ObjectId.Parse(req.addInfo["_plan_id"].ToString())},
                {"Status","1"}
            }
        };

        mongoRequest mRequest = new mongoRequest();
        mRequest.newRequestStatement(1, "e_subscription", null, null, null, documents);
        
        // Log the MongoDB insert statement
        Console.WriteLine("Inserting data into e_subscription collection...");
        mResponse = await _ds.executeStatements(mRequest, true);
         mResponse.session.CommitTransaction();
        Console.WriteLine("MongoDB insert response: " + mResponse.ToString());
        var lastId=documents[0]["_id"].ToString();
var userId=documents[0]["_user_id"].ToString();
       
            try
            {
                if (mResponse._resStatements.Count > 0)
                {
                    var logdocuments = new[]
                    {
                        new BsonDocument
                        {
                            {"_subs_id", lastId},
                            {"_entry_date", req.addInfo["_entry_date"].ToString()},
                            {"_event", "add"},
                        }
                    };

                    mongoRequest mRequest2 = new mongoRequest();
                    mongoResponse mResponse2 = new mongoResponse();
                    mRequest2.newRequestStatement(1, "e_subscription_log", null, null, null, logdocuments);
                    Console.WriteLine("Inserting data into e_subscription_log collection...");
                   mResponse2 = _ds.executeStatements(mRequest2, false).GetAwaiter().GetResult();
                    Console.WriteLine("MongoDB insert response for e_subscription_log: " + mResponse.ToString());

                    if (mResponse._resStatements.Count > 0)
                    {
                        resData.rData["rMessage"] = "Inserted Successfully";
                       //just try for status 0 in m_subscriptionuser table 
                      BsonDocument Filters1 = new BsonDocument
                {
                    {"_id", ObjectId.Parse(userId.ToString())}
                };
                
                    // Update hospital status to 1 (assuming 1 represents active)
                    BsonDocument update = new BsonDocument
                    {
                        {"$set", new BsonDocument
                            {
                                {"Status", "1"} // Assuming "Status" is part of the hospital document
                            }
                        }
                    };

                    mongoResponse updateResponse = new mongoResponse();
                    mongoRequest updateRequest = new mongoRequest();
                    updateRequest.newRequestStatement(3, "m_Subscription_Users", Filters1, null, update, null);
                    updateResponse = await _ds.executeStatements(updateRequest, false);

                    if (updateResponse != null && updateResponse._resStatements != null && updateResponse._resStatements.Count > 0)
                    {
                        resData.rData["rCode"] = 0;
                        resData.rData["rMessage"] = "User Status updated";
                    }
                   
                    }
                }
            }

                // Increment retry count
                
            
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                // You can also log the stack trace for more detailed debugging
                Console.WriteLine($"Error stack trace: {ex.StackTrace}");
            }
        }

        // Check if operation succeeded after retries
        
    
    catch (Exception ex)
    {
        resData.rStatus = 1;
        resData.rData["rCode"] = 1;
        resData.rData["rMessage"] = "Error in Inserting Data. Please try again !!!  " + ex.Message.ToString();
    }

    return resData;
}

public async Task<responseData> GetOrderHistory(requestData req)
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
                { "_user_id", ObjectId.Parse(req.addInfo["userId"].ToString()) }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "m_Subscription_Plans" },
                { "localField", "_plan_id" },//foreign key
                { "foreignField", "_id" },//primary key of m_subscription
                { "as", "joinedData" }
            }),
            new BsonDocument("$unwind", "$joinedData"),
            new BsonDocument("$project", new BsonDocument
            {
                  {"id","$_id"},
                  { "Validity", "$joinedData.validity" },
                  { "AmountPerYear", "$joinedData.amount_per_user_yearly" },
                  { "plantype", "$joinedData.plan_type" },
                  { "icon", "$joinedData.icon" },
                  { "Quantity", "$joinedData.quantity" },
                  { "plandetails", "$joinedData.plan_detail" },
                  { "totalAmount", "$joinedData.total_amount" },
                  { "startDate", "$_start_date" },
                  { "endDate", "$_end_date" },
                  { "status", "$Status" }

            })
        };

        mongoRequest mRequest = new mongoRequest();
        mongoResponse mResponse = new mongoResponse();
        mRequest.newRequestStatement(4, "e_subscription", null, null, null, pipeline);
        mResponse = await _ds.executeStatements(mRequest, false);
        var result = mResponse._resStatements[0]._selectedResults;
        if (result != null && result.Any()){
        var list = new List<Dictionary<string, object>>();
            foreach (var document in result)
            {

               
                var dict = new Dictionary<string, object>
                    {
                        { "Validity", document["Validity"].ToString() },
                        { "AmountPerYear", document["AmountPerYear"].ToString() },
                        { "plantype", document["plantype"].ToString() },
                        { "icon", document["icon"].ToString() },
                        { "Quantity", document["Quantity"].ToString() },
                        { "plandetails", document["plandetails"].ToString() },
                        { "totalAmount", document["totalAmount"].ToString() },
                        { "startDate", document["startDate"].ToString() },
                        {"endDate",document["endDate"].ToString()},
                         {"status",document["status"].ToString()},
                    };
                list.Add(dict);
        }
         
        


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
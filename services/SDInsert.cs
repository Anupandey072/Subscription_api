using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
namespace subscription_api.services
{
    public class SDInsert
    {
        private readonly dbServiceMongo _ds;
        private readonly IMongoDatabase _mongoDB;  // Assuming you have access to _mongoDB in dbServiceMongo

        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();
        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();


        public SDInsert()
        {
            _ds = new dbServiceMongo("mongodb");
            _mongoDB = _ds.GetMongoDatabase();  // Use a method to get IMongoDatabase from dbServiceMongo
        }


        public async Task RemoveExpiredEntries()
        {
            try
            {
                if (_ds != null)
                {
                    var currentDate = DateTime.UtcNow;

                    var filter = Builders<BsonDocument>.Filter.Lt("till_date", currentDate);
                    var update = Builders<BsonDocument>.Update.Set("Status", "0");

                    var bsonFilter = filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
                    var bsonUpdate = update.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);

                    var mongoRequest = new mongoRequest();
                    mongoRequest.newRequestStatement(3, "m_HospitalList", bsonFilter, null, bsonUpdate.AsBsonDocument, null);

                    await _ds.executeStatements(mongoRequest, false);
                }
                else
                {
                    Console.WriteLine("_ds is null. Make sure it is properly initialized.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveExpiredEntries: {ex.Message}");
            }
        }

        public async Task<responseData> Show_Data(requestData req)
        {
            responseData resData = new responseData();
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Show data";
            resData.eventID = req.eventID;

            try
            {
                await RemoveExpiredEntries();

                if (req.addInfo.TryGetValue("objectId", out var objectId) &&
                    req.addInfo.TryGetValue("_user_Id", out var userId))
                {
                    var selectedDocuments = await RetrieveUserDataLimited(objectId.ToString(), userId.ToString());

                    if (selectedDocuments != null && selectedDocuments.Any())
                    {
                        resData.rData["rMessage"] = "Status updated successfully in m_Hospital_List.";
                    }
                    else
                    {
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "No data found for the provided user.";
                    }
                }
                else
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Both objectId and _user_Id are required in addInfo.";
                }
            }
            catch (Exception ex)
            {
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = $"Error: {ex.Message}";
                Console.WriteLine($"Error in Show_Data: {ex.Message}");
            }
            return resData;
        }

        private async Task<List<BsonDocument>> RetrieveUserDataLimited(string objectId, string userId)
        {
            try
            {
                var objId = new ObjectId(objectId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", objId);
                var mongoRequest = new mongoRequest();
                mongoRequest.newRequestStatement(0, "m_Hospital_List", filter.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry), null, null, null);
                var mongoResponse = await _ds.executeStatements(mongoRequest, false);
                var selectedDocuments = mongoResponse._resStatements.FirstOrDefault()?._selectedResults.ToList();

                if (selectedDocuments != null && selectedDocuments.Any())
                {
                    foreach (var doc in selectedDocuments)
                    {
                        doc["Status"] = "0";
                    }

                    var subscriptionFilter = Builders<BsonDocument>.Filter.Eq("_user_id", userId);
                    var subscriptionBsonFilter = subscriptionFilter.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
                    var subscriptionRequest = new mongoRequest();
                    subscriptionRequest.newRequestStatement(0, "m_Subscription", subscriptionBsonFilter, null, null, null);
                    var subscriptionResponse = await _ds.executeStatements(subscriptionRequest, false);
                    var subscriptionDocument = subscriptionResponse._resStatements.FirstOrDefault()?._selectedResults.FirstOrDefault();

                    if (subscriptionDocument != null)
                    {
                        var fromDate = subscriptionDocument.GetValue("from_date", "").ToString();
                        var tillDate = subscriptionDocument.GetValue("till_date", "").ToString();
                        var user = subscriptionDocument.GetValue("_user_id", "").ToString();

                        if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(tillDate))
                        {
                            foreach (var doc in selectedDocuments)
                            {
                                doc["Status"] = "1";
                                doc["from_date"] = fromDate;
                                doc["till_date"] = tillDate;
                                doc["_user_id"] = user;
                            }

                            await _ds.UpdateHospitalListDocuments(selectedDocuments);

                            return selectedDocuments;
                        }
                        else
                        { throw new Exception("Invalid subscription dates in m_Subscription document."); }

                    }
                    else
                    {
                        throw new Exception("No subscription found for the provided user in m_Subscription.");
                    }
                }
                else
                {
                    throw new Exception("No data found for the provided user in m_Hospital_List.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in RetrieveUserDataLimited: {ex.Message}");
            }
        }




        public async Task<responseData> SelectData(requestData req)
        {
            mongoResponse mResponse = new mongoResponse();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Selected Data";

            try
            {
                BsonDocument filters = new BsonDocument
        {
            {"_user_id",(req.addInfo["id"].ToString())}
        };

                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(0, "m_Hospital_List", filters, null, null, null);
                mResponse = await _ds.executeStatements(mRequest, false);
                var res = mResponse._resStatements[0]._selectedResults;
                if (res.Count() > 0)
                {
                    var list = new List<object>();

                    for (var i = 0; i < res.Count(); i++)
                    {
                        var myDict = new Dictionary<string, object>();
                        foreach (var field in res[i].Names)
                        {
                            myDict[field] = res[i][field].ToString();
                        }
                        list.Add(myDict);
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


         public async Task<responseData> ChangeHospitalListStatusToZero(requestData req, string userId, string objectId)
        {
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Change Hospital List Status to 0";

            try
            {

                await UpdateHospitalListStatusToZero(userId, objectId);

                resData.eventID = req.eventID;
                resData.rData["rCode"] = 0;
                resData.rData["rMessage"] = "Successfully changed hospital list status to 0.";

            }
            catch (Exception ex)
            {
                resData.rStatus = 500; // Internal Server Error
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Internal Server Error: " + ex.Message.ToString();
            }

            return resData;
        }

        private async Task UpdateHospitalListStatusToZero(string userId, string hospitalId)
        {
            try
            {
                IMongoCollection<BsonDocument> collection = _mongoDB.GetCollection<BsonDocument>("m_Hospital_List");

                var filterBuilder = Builders<BsonDocument>.Filter;
                var filter = filterBuilder.And(
                    filterBuilder.Eq("_user_id", userId),
                    filterBuilder.Eq("_id", ObjectId.Parse(hospitalId))
                );

                var update = Builders<BsonDocument>.Update.Set("Status", "0");

                await collection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in UpdateHospitalListStatusToZero: {ex.Message}");
            }
        }

 public async Task<responseData> GetSubscriptionInfo(requestData req)
        {
            mongoResponse mResponse = new mongoResponse();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Subscription Info";

            try
            {
                // Extract userId from request
                string userId = req.addInfo["_user_Id"].ToString();

                var subscriptionFilter = Builders<BsonDocument>.Filter.Eq("_user_id", userId);
                var subscriptionRequest = new mongoRequest();
                subscriptionRequest.newRequestStatement(0, "m_Subscription", subscriptionFilter.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry), null, null, null);
                mResponse = await _ds.executeStatements(subscriptionRequest, false);
                var subscriptionDocuments = mResponse._resStatements.SelectMany(s => s._selectedResults).ToList();

                if (subscriptionDocuments.Any())
                {
                    int totalQuantity = subscriptionDocuments.Sum(doc => doc.GetValue("quantity", 0).ToInt32());
                    var hospitalFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("_user_id", userId),
                        Builders<BsonDocument>.Filter.Eq("Status", "1")
                    );

                    var hospitalRequest = new mongoRequest();
                    hospitalRequest.newRequestStatement(0, "m_Hospital_List", hospitalFilter.Render(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry), null, null, null);
                    var hospitalResponse = await _ds.executeStatements(hospitalRequest, false);
                    var hospitalCount = hospitalResponse._resStatements.SelectMany(s => s._selectedResults).Count();

                    var remainingHospitals = totalQuantity - hospitalCount;
                    var subscriptionInfo = new Dictionary<string, object>
            {
                { "TotalSubscriptionPlans", totalQuantity },
                { "SubscribedPlans", hospitalCount },
                { "RemainingSubscribedPlans", remainingHospitals },
            };
                    resData.rData["rData"] = subscriptionInfo;
                }
                else
                {
                    // Subscription not found
                    resData.rStatus = 199;
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Subscription not found for the provided user.";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                resData.rStatus = 199;
                resData.rData["rCode"] = 199;
                resData.rData["rMessage"] = "Error: " + ex.Message.ToString();
            }
            return resData;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace subscription_api.services
{
    public class hospitalList
    {
        private readonly dbServiceMongo _ds;
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();

        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        public hospitalList()
        {

            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);
        }

      
        public async Task<responseData> getHospital(requestData req)
        {
            mongoResponse mResponse = new mongoResponse();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Get All Hospital List";

            try
            {
                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(0, "m_Hospital_List", null, null, null, null);
                mResponse = await _ds.executeStatements(mRequest, false);
                var res = mResponse._resStatements[0]._selectedResults;
                var list = new List<Dictionary<string, object>>(); // Change the list type to match the data structure
                if (res != null && res.Count() > 0)
                {
                    foreach (var document in res)
                    {
                        // list.Add(document.ToDictionary());

                        var id = ObjectId.Parse(document["_id"].ToString());

                        // Create a new dictionary with ObjectId fields in the desired format
                        var hospitalData = new Dictionary<string, object>
                {
                    { "_id", id.ToString()},
                    { "Facility_Name", document["Facility_Name"].ToString() },
                    { "RC_Name", document["RC_Name"].ToString() },
                    { "Hospital_Name", document["Hospital_Name"].ToString() },
                    { "Mobile_Number", document["Mobile_Number"].ToString() },
                    { "Email_Id", document["Email_Id"].ToString() },
                    { "Status", document["Status"].ToString() }
                };

                        list.Add(hospitalData);
                    }
                }
                resData.eventID = req.eventID;
                resData.rData["rData"] = list;
            }
            catch (Exception ex)
            {
                resData.rStatus = 199;
                resData.rData["rCode"] = 199;
                resData.rData["rMessage"] = "REMOVE THIS ERROR IN PRODUCTION !!!  " + ex.Message.ToString();
            }
            return resData;
        }


        public async Task<responseData> addHospital(requestData req)
        {
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Subscription Plan Added Successfully";

            try
            {
                // Check if the user exists and has an active subscription
                BsonDocument userFilters = new BsonDocument
        {
            {"_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
        };

                mongoResponse userResponse = new mongoResponse();
                mongoRequest userRequest = new mongoRequest();
                userRequest.newRequestStatement(0, "m_Subscription_Users", userFilters, null, null, null);
                userResponse = await _ds.executeStatements(userRequest, false);
                var userResults = userResponse._resStatements[0]._selectedResults;
var userId=userResults[0]["_id"];
                if (userResults.Count > 0 && userResults[0]["Status"] == "1")
                {
                    // Retrieve plan type based on plan_id from e_subscription table
                    BsonDocument planFilters = new BsonDocument
            {
                {"_user_id", ObjectId.Parse(userId.ToString())}
            };

                    mongoResponse planResponse = new mongoResponse();
                    mongoRequest planRequest = new mongoRequest();
                    planRequest.newRequestStatement(0, "e_subscription", planFilters, null, null, null);
                    planResponse = await _ds.executeStatements(planRequest, false);
                    var planResults = planResponse._resStatements[0]._selectedResults;

                    if (planResults.Count > 0)
                    {
                        string planId = planResults[0]["_plan_id"].ToString();
                        BsonDocument planFilters1 = new BsonDocument
            {
                {"_id", ObjectId.Parse(planId.ToString())}
            };


                        mongoResponse planResponse1 = new mongoResponse();
                        mongoRequest planRequest1 = new mongoRequest();
                        planRequest1.newRequestStatement(0, "m_Subscription_Plans", planFilters1, null, null, null);
                        planResponse1 = await _ds.executeStatements(planRequest1, false);
                        var planResults1 = planResponse1._resStatements[0]._selectedResults;

                        string planType = planResults1[0]["plan_type"].ToString();

                        int maxHospitals = 0;

                        if (planType == "Basic")
                        {
                            maxHospitals = 10;
                        }
                        else if (planType == "Standard")
                        {
                            maxHospitals = 50;
                        }
                        else if (planType == "Standard")
                        {
                            maxHospitals = 300;
                        }
                        else
                        {
                            maxHospitals = 500;
                        }
                        // Add conditions for other plan types if needed

                        // Count existing hospitals for the user
                        BsonDocument hospitalFilters = new BsonDocument
                {
                    {"_user_id", ObjectId.Parse(userId.ToString())}
                };

                        mongoResponse hospitalResponse = new mongoResponse();
                        mongoRequest hospitalRequest = new mongoRequest();
                        hospitalRequest.newRequestStatement(0, "m_Hospital_List_add", hospitalFilters, null, null, null);
                        hospitalResponse = await _ds.executeStatements(hospitalRequest, false);
                        var hospitalResults = hospitalResponse._resStatements[0]._selectedResults;

                        int currentHospitals = hospitalResults.Count();
                        if (currentHospitals < maxHospitals)
                        {

                            var documents = new[]
                       {
                    new BsonDocument
                    {
                       {"_user_id", ObjectId.Parse(userId.ToString())},
                        {"_hospital_id", ObjectId.Parse(req.addInfo["hospital_id"].ToString())},
                         {"_from_Date", req.addInfo["_from_Date"].ToString()},
                          {"_to_Date", req.addInfo["_to_Date"].ToString()},
                       {"_status", req.addInfo["Status"].ToString()}
                    }
                };
                            mongoResponse updateResponse = new mongoResponse();
                            mongoRequest updateRequest = new mongoRequest();
                            updateRequest.newRequestStatement(1, "m_Hospital_List_add", null, null, null, documents);
                            updateResponse = await _ds.executeStatements(updateRequest, false);
                            // updateResponse.session.CommitTransaction();

                            if (updateResponse != null && updateResponse._resStatements != null && updateResponse._resStatements.Count > 0)
                            {
                                resData.rData["rCode"] = 0;
                                resData.rData["rMessage"] = "Hospital add Successfully";

                                //status update for hospital list 

                                BsonDocument hospitalFilters1 = new BsonDocument
                {
                    {"_id", ObjectId.Parse(req.addInfo["hospital_id"].ToString())},
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

                                mongoResponse updateResponse1 = new mongoResponse();
                                mongoRequest updateRequest1 = new mongoRequest();
                                updateRequest1.newRequestStatement(3, "m_Hospital_List", hospitalFilters1, null, update, null);
                                updateResponse1 = await _ds.executeStatements(updateRequest1, false);

                                if (updateResponse1 != null && updateResponse1._resStatements != null && updateResponse1._resStatements.Count > 0)
                                {
                                    resData.rData["rCode"] = 0;
                                    resData.rData["rMessage"] = "Hospital status Updated";
                                }
                            }
                            else
                            {
                                resData.rStatus = 199;
                                resData.rData["rCode"] = 1;
                                resData.rData["rMessage"] = "Failed to add Hospital ";
                            }
                        }
                        else
                        {
                            resData.rStatus = 199;
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = $"Maximum allowed hospitals ({maxHospitals}) reached for the subscription plan type.";
                        }
                    }
                    else
                    {
                        resData.rStatus = 199;
                        resData.rData["rCode"] = 1;
                        resData.rData["rMessage"] = "Subscription Plan Not Found";
                    }
                }
                else
                {
                    resData.rStatus = 199;
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "User Not Found or User Subscription is Inactive";
                }
            }
            catch (Exception ex)
            {
                resData.rStatus = 199;
                resData.rData["rCode"] = 199;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                Console.WriteLine("Error in addHospital: " + ex.ToString());
            }

            return resData;
        }
        //subscribed hospital history 

        public async Task<responseData> getAllsubscribedHospital(requestData req)
        {

            mongoResponse mResponse = new mongoResponse();
            mongoRequest mRequest = new mongoRequest();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = " All Subscribed Hospital List displayed sucessfully";
            try
            {

                var pipeline = new BsonDocument[]
           {
            new BsonDocument("$match", new BsonDocument
            {
             {"_user_id", ObjectId.Parse(req.addInfo["_user_id"].ToString())},
            }),
            // new BsonDocument("$lookup", new BsonDocument
            // {
            //     { "from", "m_Subscription_Users" },
            //     { "localField", "_user_id" },
            //     { "foreignField", "_id" },
            //     { "as", "joinedPat" }
            // }),
            // new BsonDocument("$unwind", "$joinedPat"),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "m_Hospital_List" },
                { "localField", "_hospital_id" },
                { "foreignField", "_id" },
                { "as", "joinedData" }
            }),
            new BsonDocument("$unwind", "$joinedData"),

            new BsonDocument("$project", new BsonDocument
            {
                { "_id", "$joinedData._id" },
                 { "Facility_Name", "$joinedData.Facility_Name" },
                 { "RC_Name", "$joinedData.RC_Name" },
                 { "Hospital_Name", "$joinedData.Hospital_Name" },
                 { "Mobile_Number", "$joinedData.Mobile_Number" },
                 { "Email_Id", "$joinedData.Email_Id" },
                 { "Status", "$joinedData.Status" },
                 { "_status", "$_status" },
                 { "_from_Date", "$_from_Date" },
                 { "_to_Date", "$_to_Date" }

            })
           };

                mRequest.newRequestStatement(4, "m_Hospital_List_add", null, null, null, pipeline);
                mResponse = await _ds.executeStatements(mRequest, false);
                var result = mResponse._resStatements[0]._selectedResults;
                  for (var k = 0; k < result.Count(); k++)
            {
            if (result != null && result[k]["_status"] == "1" )
                {
                    var list = new List<Dictionary<string, object>>();
                    foreach (var document in result)
                    {
                        var dict = new Dictionary<string, object>
                    {
                        { "_id", document["_id"].ToString() },
                        { "Facility_Name", document["Facility_Name"].ToString() },
                        { "RC_Name", document["RC_Name"].ToString() },
                        { "Hospital_Name", document["Hospital_Name"].ToString() },
                        { "Mobile_Number", document["Mobile_Number"].ToString() },
                        { "Email_Id", document["Email_Id"].ToString() },
                        { "Status", document["Status"].ToString() },
                         { "_status", document["_status"].ToString() },
                        { "_from_Date", document["_from_Date"].ToString() },
                        { "_to_Date", document["_to_Date"].ToString() },
                    };
                        list.Add(dict);
                    }
                    resData.eventID = req.eventID;
                    resData.rData["rData"] = list;

                }
                else{
                resData.rData["rCode"] = 2;
                resData.rData["rMessage"] = "No data available";
                }
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

        //remove hospital


        public async Task<responseData> removeHospital(requestData req)
        {
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Hospital removed successfully";

            try
            {
                // Check if the user exists and has an active subscription
                BsonDocument userFilters = new BsonDocument
        {
            {"_user_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
            {"_hospital_id", ObjectId.Parse(req.addInfo["hospitalid"].ToString())},
        };

                mongoResponse userResponse = new mongoResponse();
                mongoRequest userRequest = new mongoRequest();
                userRequest.newRequestStatement(0, "m_Hospital_List_add", userFilters, null, null, null);
                userResponse = await _ds.executeStatements(userRequest, false);
                var userResults = userResponse._resStatements[0]._selectedResults;


                if (userResults.Count > 0)
                {
                    BsonDocument Filterslist = new BsonDocument
                   {
                        {"_id", ObjectId.Parse(req.addInfo["hospitalid"].ToString())},
                    };

                    //check hospital list status 

                    mongoResponse userResponse1 = new mongoResponse();
                    mongoRequest userRequest1 = new mongoRequest();
                    userRequest1.newRequestStatement(0, "m_Hospital_List", Filterslist, null, null, null);
                    userResponse1 = await _ds.executeStatements(userRequest1, false);
                    var userResults1 = userResponse1._resStatements[0]._selectedResults;

                    if (userResults1.Count > 0 && userResults1[0]["Status"] == "1")
                    {
                        BsonDocument update = new BsonDocument
                    {
                        {"$set", new BsonDocument
                            {
                                {"Status", "0"} // Assuming "Status" is part of the hospital document
                            }
                        }
                    };


                        mongoResponse updateResponse1 = new mongoResponse();
                        mongoRequest updateRequest1 = new mongoRequest();
                        updateRequest1.newRequestStatement(3, "m_Hospital_List", Filterslist, null, update, null);
                        updateResponse1 = await _ds.executeStatements(updateRequest1, false);
                        if (updateResponse1 != null && updateResponse1._resStatements != null && updateResponse1._resStatements.Count > 0)
                        {
                             BsonDocument Filtersadd = new BsonDocument
                   {
                        {"_hospital_id", ObjectId.Parse(req.addInfo["hospitalid"].ToString())},
                    };
                            //try for hospitallist add table status 
                            BsonDocument updatestatus = new BsonDocument
                    {
                        {"$set", new BsonDocument
                            {
                                {"Status", "0"} // Assuming "Status" is part of the hospital document
                            }
                        }
                    };
                            mongoResponse updateResponse11 = new mongoResponse();
                            mongoRequest updateRequest11 = new mongoRequest();
                            updateRequest11.newRequestStatement(3, "m_Hospital_List_add", Filtersadd, null, updatestatus, null);
                            updateResponse11 = await _ds.executeStatements(updateRequest11, false);
                            if (updateResponse11 != null && updateResponse11._resStatements != null && updateResponse11._resStatements.Count > 0)

                            {

                                resData.rData["rCode"] = 0;
                                resData.rData["rMessage"] = "Hospital Unsubscribed";
                            }
                            else
                            {
                                resData.rStatus = 199;
                                resData.rData["rCode"] = 1;
                                resData.rData["rMessage"] = "Failed to Unsubscribed ";
                            }
                        }
                        else
                        {
                            resData.rStatus = 199;
                            resData.rData["rCode"] = 1;
                            resData.rData["rMessage"] = "Failed .................";
                        }



                    }

                }
                else
                {
                    resData.rStatus = 199;
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "You can not valid user for unsubscribed ";
                }


            }

            catch (Exception ex)
            {
                resData.rStatus = 199;
                resData.rData["rCode"] = 199;
                resData.rData["rMessage"] = "An error occurred: " + ex.Message;
                Console.WriteLine("Error in addHospital: " + ex.ToString());
            }

            return resData;
        }
//for total subscribe users data show in dashboard
      public async Task<responseData> subscribelist(requestData req)
{
    responseData resData = new responseData();
    resData.rStatus = 0;
    resData.rData["rCode"] = 0;
    resData.rData["rMessage"] = "Data Displayed Successfully";

    try
    {
        // Check if the user exists and has an active subscription
        BsonDocument userFilters = new BsonDocument
        {
            {"_id", ObjectId.Parse(req.addInfo["user_id"].ToString())},
        };

        mongoResponse userResponse = new mongoResponse();
        mongoRequest userRequest = new mongoRequest();
        userRequest.newRequestStatement(0, "m_Subscription_Users", userFilters, null, null, null);
        userResponse = await _ds.executeStatements(userRequest, false);
        var userResults = userResponse._resStatements[0]._selectedResults;
        var userId = userResults[0]["_id"];
        if (userResults.Count > 0 && userResults[0]["Status"] == "1")
        {
            // Retrieve all plans for the user
            BsonDocument planFilters = new BsonDocument
            {
                {"_user_id", ObjectId.Parse(userId.ToString())}
            };

            mongoResponse planResponse = new mongoResponse();
            mongoRequest planRequest = new mongoRequest();
            planRequest.newRequestStatement(0, "e_subscription", planFilters, null, null, null);
            planResponse = await _ds.executeStatements(planRequest, false);
            var planResults = planResponse._resStatements[0]._selectedResults;

            if (planResults.Count > 0)
            {
                int totalCount = 0;

                foreach (var plan in planResults)
                {
                    string planId = plan["_plan_id"].ToString();
                    BsonDocument planFilters1 = new BsonDocument
                    {
                        {"_id", ObjectId.Parse(planId)}
                    };

                    mongoResponse planResponse1 = new mongoResponse();
                    mongoRequest planRequest1 = new mongoRequest();
                    planRequest1.newRequestStatement(0, "m_Subscription_Plans", planFilters1, null, null, null);
                    planResponse1 = await _ds.executeStatements(planRequest1, false);
                    var planResults1 = planResponse1._resStatements[0]._selectedResults;

                    if (planResults1.Count > 0)
                    {
                        string planType = planResults1[0]["plan_type"].ToString();
                        int maxHospitals = GetMaxHospitalsForPlan(planType); // Method to get max hospitals based on plan type
                        totalCount += maxHospitals;
                    }
                }

                // Count existing hospitals for the user with status = 1
                BsonDocument hospitalFilters = new BsonDocument
                {
                     {"_user_id", ObjectId.Parse(userId.ToString())},
                    {"_status","1"} // Filter by status = 1
                };

                mongoResponse hospitalResponse = new mongoResponse();
                mongoRequest hospitalRequest = new mongoRequest();
                hospitalRequest.newRequestStatement(0, "m_Hospital_List_add", hospitalFilters, null, null, null);
                hospitalResponse = await _ds.executeStatements(hospitalRequest, false);
                var hospitalResults = hospitalResponse._resStatements[0]._selectedResults;

                int currentHospitals = hospitalResults.Count();

                resData.rData["rtotalHospitals"] = totalCount;
                resData.rData["rCurrentHospitals"] = currentHospitals;
            }
            else
            {
                resData.rStatus = 199;
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Subscription Plans Not Found";
            }
        }
        else
        {
            resData.rStatus = 199;
            resData.rData["rCode"] = 1;
            resData.rData["rMessage"] = "User Not Found or User Subscription is Inactive";
        }
    }
    catch (Exception ex)
    {
        resData.rStatus = 199;
        resData.rData["rCode"] = 199;
        resData.rData["rMessage"] = "An error occurred: " + ex.Message;
        Console.WriteLine("Error in addHospital: " + ex.ToString());
    }

    return resData;
}

private int GetMaxHospitalsForPlan(string planType)
{
    switch (planType)
    {
        case "Basic":
            return 10;
        case "Standard":
            return 50;
        case "Premium":
            return 300;
        default:
            return 500;
    }
}


    }
}
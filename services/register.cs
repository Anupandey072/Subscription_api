using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;
namespace subscription_api
{
    public class register
    {
        private readonly dbServiceMongo _ds; // this can be changed if more connections are required by this service like below
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();
        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        public register()
        {
            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);

        }

        // Anu's updated code
        public async Task<responseData> registration(requestData req)
        {
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "User Registration Successfully";
            //code 
            mongoResponse mResponse1 = new mongoResponse();
            BsonDocument filters = new BsonDocument
                {
                    { "$or", new BsonArray
                        {
                            new BsonDocument { { "_mobile_no", req.addInfo["_mobile_no"].ToString() } },
                            new BsonDocument { { "_email_id", req.addInfo["_email_id"].ToString() } }
                        }
                    }
                };

            mongoRequest mRequest1 = new mongoRequest();
            mRequest1.newRequestStatement(0, "m_Subscription_Users", filters, null, null, null);
            mResponse1 = _ds.executeStatements(mRequest1, false).GetAwaiter().GetResult();
            var existingUsers = mResponse1._resStatements[0]._selectedResults;

            if (existingUsers.Any())
            {
                resData.rData["rCode"] = 2;
                resData.rData["rMessage"] = "mobile or email already exists.";
                return resData;
            }

            // code
            mongoResponse mResponse = new mongoResponse();
            try
            {
                var documents = new[]
                {
                    new BsonDocument
                    {
                        { "_name", req.addInfo["_name"].ToString()},
                        {"_mobile_no", req.addInfo["_mobile_no"].ToString()},
                        {"_email_id", req.addInfo["_email_id"].ToString()},
                        {"_pin_code", req.addInfo["_pin_code"].ToString()},
                        {"_address", req.addInfo["_address"].ToString()},
                        {"_password", req.addInfo["_password"].ToString()},
                        {"Status","0"}
                    }
                };

                mongoRequest mRequest11 = new mongoRequest();
                mRequest11.newRequestStatement(1, "m_Subscription_Users", null, null, null, documents);
                mResponse = await _ds.executeStatements(mRequest11, true);
                mResponse.session.CommitTransaction();

                var mergedCertsArray = new BsonArray();
                var existingCertsArray = JArray.Parse(req.addInfo["document"].ToString());
                foreach (var cert in existingCertsArray)
                {
                    cert["doc_id"]=ObjectId.GenerateNewId().ToString();
                    mergedCertsArray.Add(BsonDocument.Parse(cert.ToString()));
                    

                }
                var documentsData = new[]
                        {
                    new BsonDocument
                    {
                        {"_user_id", ObjectId.Parse(documents[0]["_id"].ToString())},
                        {"document",mergedCertsArray}
                    }
                };
                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(1, "m_Subscription_user_Doc", null, null, null, documentsData);
                await _ds.executeStatements(mRequest, false);
            }
            catch (Exception ex)
            {
                resData.rStatus = 1;
                resData.rData["rCode"] = 1;
                resData.rData["rMessage"] = "Error in Inserting Data. Please try again !!!  " + ex.Message.ToString();
            }

            return resData;
        }
      
    }
    }
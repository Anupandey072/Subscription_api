using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;

namespace subscription_api.services
{

    // first put your connection strings in appsettings.json
    public class login
    {
        private readonly dbServiceMongo _ds; // this can be changed if more connections are required by this service like below
        private readonly Dictionary<string, string> _service_config = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _jwt_config = new Dictionary<string, string>();
        IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        public login()
        {
            _ds = new dbServiceMongo("mongodb");
            appCommonFunctions.createDB(_ds, _service_config);

        }

        public async Task<responseData> loginUser(requestData req)
        {
            mongoResponse mResponse = new mongoResponse();
            responseData resData = new responseData();
            resData.rStatus = 0;
            resData.rData["rCode"] = 0;
            resData.rData["rMessage"] = "Login successful";

            try
            {
                if (!req.addInfo.ContainsKey("Email_Id") || !req.addInfo.ContainsKey("Password"))
                {
                    resData.rData["rCode"] = 1;
                    resData.rData["rMessage"] = "Invalid request. Please provide email and password.";
                    return resData;
                }
                BsonDocument filters = new BsonDocument
        {
            { "_email_id", req.addInfo["Email_Id"].ToString() },
            { "_password", req.addInfo["Password"].ToString() }
        };

                mongoRequest mRequest = new mongoRequest();
                mRequest.newRequestStatement(0, "m_Subscription_Users", filters, null, null, null);
                mResponse = await _ds.executeStatements(mRequest, false);
                var user = mResponse._resStatements[0]._selectedResults.FirstOrDefault();

                if (user != null)
                {
                    BsonDocument subscriptionFilters = new BsonDocument
            {
                { "_user_id", user["_id"].ToString() }
            };

                    mongoRequest subscriptionRequest = new mongoRequest();
                    subscriptionRequest.newRequestStatement(0, "m_Subscription", subscriptionFilters, null, null, null);
                    mResponse = await _ds.executeStatements(subscriptionRequest, false);
                    var subscription = mResponse._resStatements[0]._selectedResults.FirstOrDefault();
                    if (subscription != null && subscription["status"].ToString() == "1")
                    {
                        resData.rData["_subscription_status"] = "1";
                        resData.rData["rMessage"] = "User is subscribed and logged in successfully.";
                    }
                    else
                    {
                        resData.rData["_subscription_status"] = "0";
                        resData.rData["rMessage"] = "User is not subscribed.";
                    }
                    BsonDocument updateFilter = new BsonDocument { { "_id", user["_id"].ToString() } };
                    BsonDocument updateDocument = new BsonDocument { { "_subscription_status", resData.rData["_subscription_status"].ToString() } };

                    mongoRequest updateRequest = new mongoRequest();
                    updateRequest.newRequestStatement(2, "m_Subscription_Users", updateFilter, updateDocument, null, null);
                    mResponse = await _ds.executeStatements(updateRequest, false);

                    var claims = new[]
                    {
                new Claim(ClaimTypes.Email, user["_email_id"].ToString())
            };

                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appsettings["Jwt:Key"]));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
                    var tokenDescriptor = new JwtSecurityToken(
                        issuer: appsettings["Jwt:Issuer"],
                        audience: appsettings["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.Now.AddMinutes(Convert.ToDouble(appsettings["Jwt:ExpireMinutes"])),
                        signingCredentials: credentials);

                    var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
                    resData.rData["jwt"] = token;
                    resData.rData["objectId"] = user["_id"].ToString(); // Add ObjectId to response
                }
                else
                {
                    resData.rData["rCode"] = 2;
                    resData.rData["rMessage"] = "User not found or invalid credentials.";
                }
            }
            catch (Exception ex)
            {
                resData.rStatus = 500;
                resData.rData["rCode"] = 500;
                resData.rData["rMessage"] = "An error occurred while processing the request: " + ex.Message;
            }

            return resData;
        }


    }
}
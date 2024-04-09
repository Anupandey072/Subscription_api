using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net.Http;
using System.Text;

public static class appCommonFunctions
{
    public static async Task createDB(dbServiceMongo db, Dictionary<string, string> _sConfig)
    {
        try
        {
            // create indexes for email table where email_id is indexed with unique criteria
            // if only_mobile or default
            var indexesJson = @"[
                { 'keys': { 'mobile_no': 1 }, 'options': { 'unique': true } },
                { 'keys': { 'expireAt': 1 }, 'options': { 'unique': false }, 'partialFilterExpression': { 'status': 0 }, 'expireAfterSeconds': 60 }
                ]";

            //             var indexesJson = @"[{
            //     ""expireAfterSeconds"": 3600,
            //     ""partialFilterExpression"": {
            //         ""status"": 0
            //     },
            //     ""options"": { 'unique': false },
            //     ""keys"": {
            //         ""expireAt"": 1
            //     }
            // }]";

            //var indexesJson = @"[
            //     { 'keys': { 'expireAt': 1 }, 'partialFilterExpression': { 'status': 0 }, 'expireAfterSeconds' : 3600 }
            // ]"; 
            var requiredFields = "['full_name', 'country_code', 'mobile_no','guid','pass']";
            string jsonSchemaString = $@"{{
            'bsonType': 'object',
            'required': {requiredFields}, 
            'properties': {{             
        
            }}
            }}";

            if (_sConfig["auth_fields_required"] == "only_email") // only email id
            {
                indexesJson = @"[
                { 'keys': { 'email_id': 1 }, 'options': { 'unique': true } }
                ]";

                // indexesJson = @"[
                //     { 'keys': { 'email_id': 1 }, 'options': { 'unique': true } },
                //     { 'keys': { 'deleteAfter': 1 }, 'partialFilterExpression': { 'status': 0 }, 'expireAfterSeconds' : 3600 }
                // ]"; 
                requiredFields = "['full_name', 'country_code', 'email_id','guid','pass']";

                jsonSchemaString = $@"{{
                'bsonType': 'object',
                'required': {requiredFields}, 
                'properties': {{                            
                    'email_id': {{'bsonType': 'string',
                        'pattern': '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{{2,}}$',
                        'description': 'The \'email\' field must be a valid email address.'
                    }}
                }}
                }}";
            }
            else if (_sConfig["auth_fields_required"] == "email_and_mobile") // email_and_mobile
            {
                indexesJson = @"[
                { 'keys': { 'mobile_no': 1 }, 'options': { 'unique': true } },
                { 'keys': { 'email_id': 1 }, 'options': { 'unique': true } }
                ]";
                requiredFields = "['full_name', 'country_code','mobile_no','email_id','guid','pass']";
                jsonSchemaString = $@"{{
                'bsonType': 'object',
                'required': {requiredFields}, 
                'properties': {{                          
                    'email_id': {{'bsonType': 'string',
                        'pattern': '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{{2,}}$',
                        'description': 'The \'email\' field must be a valid email address.'
                    }}
                }}
                }}";
            }

            await db.CreateMultipleIndexesAsync(indexesJson, "(reg_auth_svc)-(auth_master)");

            // create for email_messages
            // otp type should have values 1=registration_otp, 2=authentication_otp, 


            await db.SetCollectionValidationRuleAsync(jsonSchemaString, "(reg_auth_svc)-(auth_master)");

        }
        catch (Exception ex)
        {
            throw;
        }

    }

    public static async Task<responseData> requestPOST_API(String apiUrl, String jsonBody)
    {
        using (var client = new HttpClient())
        {
            // Set the API endpoint URL
            //string apiUrl = "https://example.com/api/users";

            // Create the request body as a JSON string
            //string jsonBody = "{\"username\": \"johndoe\", \"password\": \"password123\"}";

            // Create the request content as a StringContent object
            var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Send the HTTP POST request and get the response
            var response = await client.PostAsync(apiUrl, requestContent);

            // Read the response content as a JSON object
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<responseData>(jsonResponse, jsonOptions);

        }
    }


}
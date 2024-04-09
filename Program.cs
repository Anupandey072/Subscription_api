using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http; // added by me
using Microsoft.Net.Http.Headers; // added by me
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.IO;
using System.Collections;
using subscription_api.services;
using subscription_api;


WebHost.CreateDefaultBuilder().
ConfigureServices(s =>
{

    IConfiguration appsettings = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();


    s.AddSingleton<subscription>();
    s.AddSingleton<register>();
    s.AddSingleton<login>();
    s.AddSingleton<hospitalList>();
    s.AddSingleton<SDInsert>();
    s.AddSingleton<GetAll>();
    s.AddSingleton<updateUser>();
    s.AddAuthorization();
    s.AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = appsettings["Jwt:Issuer"],
            ValidAudience = appsettings["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appsettings["Jwt:Key"]))
        };
    });


    s.AddCors(options =>
                    {
                        options.AddPolicy("AllowAnyOrigin",
                            builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyHeader()
                                       .AllowAnyMethod();
                            });
                    });
    s.AddHttpClient<TestServiceRequest>(); // this is to access access api server to server
    s.AddControllers();



}).
Configure(app =>
{
    //app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();


    app.UseAuthentication();
    app.UseAuthorization(); // this line has to apperar between app.UseRouting and app.UseEndPoints

    //app.UseCors(MyAllowSpecificOrigins);

    app.UseCors("AllowAnyOrigin");



    app.UseEndpoints(e =>
    {


        var Details = e.ServiceProvider.GetRequiredService<subscription>();
        var reg = e.ServiceProvider.GetRequiredService<register>();
        var log = e.ServiceProvider.GetRequiredService<login>();
        var lists = e.ServiceProvider.GetRequiredService<hospitalList>();
         var sta = e.ServiceProvider.GetRequiredService<SDInsert>();
          var getall = e.ServiceProvider.GetRequiredService<GetAll>();
            var update = e.ServiceProvider.GetRequiredService<updateUser>();
        try
        {

            e.MapGet("/test",
                         async c => await c.Response.WriteAsJsonAsync("Hello Word!.."));


             e.MapPost("subscription",
       [AllowAnonymous] async (HttpContext http) =>
       {
           var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
           requestData rData = JsonSerializer.Deserialize<requestData>(body);

           string objectId = ""; // Initialize the objectId

           // Extract objectId from addInfo if available
           if (rData.addInfo != null && rData.addInfo.ContainsKey("_id"))
           {
               objectId = rData.addInfo["_id"].ToString();
           }

           if (rData.eventID == "1001")
               await http.Response.WriteAsJsonAsync(await Details.addDetails(rData, objectId));
                if (rData.eventID == "1006")
               await http.Response.WriteAsJsonAsync(await Details.GetSubscriptionPlan(rData));
                if (rData.eventID == "1007")
               await http.Response.WriteAsJsonAsync(await Details.PurchaseSubscription(rData));
                if (rData.eventID == "1008")
               await http.Response.WriteAsJsonAsync(await Details.GetOrderHistory(rData));
       });

           
            


            e.MapPost("registration",
         [AllowAnonymous] async (HttpContext http) =>
         {

             var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
             requestData rData = JsonSerializer.Deserialize<requestData>(body);
                 if (rData.eventID == "1002")
                 await http.Response.WriteAsJsonAsync(await reg.registration(rData));

         });
            e.MapPost("login",
           [AllowAnonymous] async (HttpContext http) =>
           {

               var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
               requestData rData = JsonSerializer.Deserialize<requestData>(body);
               if (rData.eventID == "1001")
                   await http.Response.WriteAsJsonAsync(await log.loginUser(rData));

           });
            e.MapPost("dashboard",
           [AllowAnonymous] async (HttpContext http) =>
           {

               var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
               requestData rData = JsonSerializer.Deserialize<requestData>(body);
               if (rData.eventID == "1001")
                   await http.Response.WriteAsJsonAsync(await getall.Dashboard(rData));

           });
            e.MapPost("updateUser",
           [AllowAnonymous] async (HttpContext http) =>
           {

               var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
               requestData rData = JsonSerializer.Deserialize<requestData>(body);
               if (rData.eventID == "1001")
                   await http.Response.WriteAsJsonAsync(await update.updateUserDoc(rData));
                    if (rData.eventID == "1002")
                   await http.Response.WriteAsJsonAsync(await update.updateUsers(rData));
                     if (rData.eventID == "1003")
                   await http.Response.WriteAsJsonAsync(await update.updatePassword(rData));

           });



       e.MapPost("hospitaladd",
     [AllowAnonymous] async (HttpContext http) =>
     {
         var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
         requestData rData = JsonSerializer.Deserialize<requestData>(body);
         if (rData.eventID == "1001")
         {
             await http.Response.WriteAsJsonAsync(await lists.addHospital(rData));
         }
         if (rData.eventID == "1002")
         {
             await http.Response.WriteAsJsonAsync(await lists.getHospital(rData));
         }
         if (rData.eventID == "1003")
         {
             await http.Response.WriteAsJsonAsync(await lists.getAllsubscribedHospital(rData));
         }
         if (rData.eventID == "1004")
         {
             await http.Response.WriteAsJsonAsync(await lists.removeHospital(rData));
         }
         if (rData.eventID == "1005")
         {
             await http.Response.WriteAsJsonAsync(await lists.subscribelist(rData));
         }
         
     });





  e.MapPost("subscribedHospitalList",
            [AllowAnonymous] async (HttpContext http) =>
            {

                var body = await new StreamReader(http.Request.Body).ReadToEndAsync();
                requestData rData = JsonSerializer.Deserialize<requestData>(body);
                if (rData.eventID == "1001")
                    await http.Response.WriteAsJsonAsync(await sta.Show_Data(rData));
                if (rData.eventID == "1002")
                    await http.Response.WriteAsJsonAsync(await sta.SelectData(rData));
                if (rData.eventID == "1003")
                {
                    // Extracting _user_id from addInfo
                    string userId = rData.addInfo["_user_id"].ToString();
                    string objId = rData.addInfo["_id"].ToString();
                    // Calling SelectData with extracted userId
                    await http.Response.WriteAsJsonAsync(await sta.ChangeHospitalListStatusToZero(rData, userId, objId));
                }
                 if (rData.eventID == "1004")
                    await http.Response.WriteAsJsonAsync(await sta.GetSubscriptionInfo(rData));
                });




            e.MapGet("/bing",

                async c => await c.Response.WriteAsJsonAsync("{'Name':'Pravin','Age':'43'}"));
            //e.MapGet("/contacts/{id:int}",

            e.MapPost("/bing",

                async c => await c.Response.WriteAsJsonAsync("{'Name':'Pravin POST','Age':'43 POST'}"));

            e.MapDefaultControllerRoute();

        }
        catch (Exception ex)
        {
            Console.Write(ex);
        }

    });
}).Build().Run();

public record requestData
{ //request data
  //SOURCE.srv.fn_CS({ rID: "F000", rData: {encData: encrypted}}, page_OS, $("#progressBarFooter")[0]);
    [Required]
    public string eventID { get; set; } //  request ID this is the ID of entity requesting the API (UTI/CDAC/CAC) this is used to pick up the respective private key for the requesting user


    [Required]
    public IDictionary<string, object> addInfo { get; set; } // request data .. previously addInfo 
}

public record responseData
{ //response data
    public responseData()
    { // set default values here
        eventID = "";
        rStatus = 0;
        rData = new Dictionary<string, object>();

    }
    [Required]
    public int rStatus { get; set; } = 0; // this will be defaulted 0 fo success and other numbers for failures
    [Required]
    public string eventID { get; set; } //  response ID this is the ID of entity requesting the
    public IDictionary<string, object> addInfo { get; set; } // request data .. previously addInfo 
    public Dictionary<string, object> rData { get; set; }
    //public ArrayList rData {get;set;}
}

public class TestServiceRequest
{
    private readonly HttpClient _httpClient;


    // returns a JSON String 
    public String executeSQL(String sql, String prm)
    {
        return "";
    }

    public TestServiceRequest(HttpClient httpClient)
    {
        var _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:5002/");

    }

    public async Task<String> GetAllContacts() // this function is called when ever a mapped link is typed
    {
        // get sql data here
        MySqlConnection conn = null;
        String s = "";
        var sb = new MySqlConnectionStringBuilder
        {
            Server = "127.0.0.1",
            UserID = "root",
            Password = "admin*123",
            Port = 3306,
            Database = "leads"
        };

        try
        {
            Console.WriteLine(sb.ConnectionString);
            conn = new MySqlConnection(sb.ConnectionString);
            conn.Open();
            MySqlTransaction t = conn.BeginTransaction();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM test;";
            var reader = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
            //String s ="";
            while (reader.Read())
            {
                s = s + " " + reader.GetInt32("id") + " " + reader.GetString("Name") + "\n";
            }
        }
        catch (MySqlException ex)
        {
            Console.Write(ex.Message);
        }
        finally
        {
            if (conn != null)
                conn.Close();
        }
        // sql test ends here



        String x = "";//await _httpClient.GetStringAsync("contacts");
        return x + "ADDED STRING FROM DB" + s;
    }

}
using Dapper;
using Firebase.Database;
using Firebase.Database.Query;
using FQSAPI.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.SignalR;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using LicenseContext = OfficeOpenXml.LicenseContext;
using System.Linq;


namespace FQSAPI.Controllers
{
    [ApiController]
    [Route("FireBaseFQS")]
    public class FireBaseFQSController : ControllerBase
    {
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly FirebaseClient _firebaseClient;
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        static string connString = @"data source = localhost\MSSQLSERVER2; user id = sa; password = D3f@ult!; initial catalog = FQS ;";
        SqlConnection conn = new SqlConnection(connString);

        private readonly string _connectionString;

        public FireBaseFQSController(IHubContext<QueueHub> hubContext, FirebaseClient firebaseClient, IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _firebaseClient = firebaseClient;
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient(); // Proper initialization
        }

        [HttpPost("AddQueueCode")]
        public async Task<ActionResult<string>> AddQueueCode()
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Get today's date code
                string todayDateCode = DateTime.Today.ToString("yyyyMMdd");

                // Find the maxQueueCode (simpler approach)
                int maxQueueCode = allItems
                    .Where(item => item.Object != null)
                    .Where(item => item.Object.dateCode?.ToString() == todayDateCode)
                    .Select(item => item.Object.queueCode)
                    .Where(queueCode => queueCode != null)
                    .Select(queueCode =>
                    {
                        int.TryParse(queueCode.ToString(), out int result);
                        return result;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                int nextQueueCode;

                // Check if we have any items for today
                if (maxQueueCode == 0)
                {
                    nextQueueCode = 1;
                }
                else
                {
                    nextQueueCode = maxQueueCode + 1;
                }

                // Create an object to store in Firebase
                var queueData = new
                {
                    queueCode = nextQueueCode,
                    dateCode = todayDateCode,
                    timeCreated = DateTime.Now,
                    status = "Pending"
                };

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblQueue")
                    .PostAsync(queueData);

                // Send SignalR message to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", nextQueueCode + ":" + 1);

                return Ok(nextQueueCode+":"+1);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("UpdateQueueCode")]
        public async Task<ActionResult<string>> UpdateQueueCode([FromBody] FirebaseUpdateModel request)
        {
            try
            {
                string newFieldValue = "";
                string newStatusValue = "";
                if (request.NewStatus == 2)
                {
                    newFieldValue = "TimeStarted";
                    newStatusValue = "In Progress";
                }
                else if (request.NewStatus == 3)
                {
                    newFieldValue = "TimeCompleted";
                    newStatusValue = "Ready";
                }
                else if (request.NewStatus == 4)
                {
                    newFieldValue = "TimeCleared";
                    newStatusValue = "Done";
                }

                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Find the item with matching queueCode
                var targetItem = allItems.FirstOrDefault(item =>
                    item.Object != null &&
                    item.Object.queueCode == request.QueueCode);

                if (targetItem != null)
                {
                    await _firebaseClient
                        .Child("tblQueue")
                        .Child(targetItem.Key)
                        .Child("status")
                        .PutAsync(JsonConvert.SerializeObject(newStatusValue));

                    var newField = newFieldValue;

                    // Create a dictionary to dynamically set the field name
                    var updateData = new Dictionary<string, object>
                    {
                        { newField, DateTime.Now } // Use the value as field name
                    };

                    await _firebaseClient
                        .Child("tblQueue")
                        .Child(targetItem.Key)
                        .PatchAsync(updateData);

                    // Send SignalR message to all connected clients
                    await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", request.QueueCode + ":" + request.NewStatus);

                    return Ok("QueueCode status updated successfully!");
                }
                else
                {
                    return NotFound($"Queue code {request.QueueCode} not found");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetActiveQueue")]
        public async Task<ActionResult<ActiveQueueModel>> GetActiveQueue()
        {
            try
            {
                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                var queued = new List<int>();
                //queued = allItems where status = "Pending";

                var started = new List<int>();
                //started = allItems where status = "In Progress";

                var done = new List<int>();
                //done = allItems where status = "Completed";

                foreach (var item in allItems)
                {
                    if(item.Object == null)
                    {
                        continue;
                    }
                    else
                    {
                        int queueCode = Int32.Parse(item.Object.queueCode.ToString());
                        string status = item.Object.status.ToString();

                        switch (status.ToLower())
                        {
                            case "pending":
                                queued.Add(queueCode);
                                break;
                            case "in progress":
                                started.Add(queueCode);
                                break;
                            case "ready":
                                done.Add(queueCode);
                                break;
                        }
                    }
                }

                return Ok(new ActiveQueueModel
                {
                    Queued = queued,
                    InProgress = started,
                    Completed = done
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("CreateMenuItem")]
        public async Task<ActionResult<ActiveQueueModel>> CreateMenuItem(MenuModel data)
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblMenu")
                    .OnceAsync<dynamic>();

                // Find the max ID
                int maxID = allItems
                    .Where(item => item.Object != null)
                    .Select(item => item.Object.ID)
                    .Where(ID => ID != null)
                    .Select(ID =>
                    {
                        return int.Parse(ID.ToString());
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                //set Data ID
                data.ID = maxID+1;

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblMenu")
                    .PostAsync(data);

                return Ok("Menu item has been added!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetMenuList")]
        public async Task<ActionResult<ActiveQueueModel>> GetMenuList()
        {
            try
            {
                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblMenu")
                    .OnceAsync<dynamic>();

                var menuList = new List<MenuModel>();

                foreach (var item in allItems)
                {
                    if (item.Object == null)
                    {
                        continue;
                    }
                    else
                    {
                        var data = new MenuModel
                        {
                            ID = item.Object.ID,
                            Name = item.Object.Name,
                            Price = item.Object.Price,
                            Image = item.Object.Image
                        };

                        menuList.Add(data);
                    }
                }

                return Ok(menuList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("SubmitOrder")]
        public async Task<ActionResult<ActiveQueueModel>> SubmitOrder([FromBody] List<OrderItemModel> itemsList)
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Get today's date code
                string todayDateCode = DateTime.Today.ToString("yyyyMMdd");

                // Find the maxQueueCode (simpler approach)
                int maxQueueCode = allItems
                    .Where(item => item.Object != null)
                    .Where(item => item.Object.dateCode?.ToString() == todayDateCode)
                    .Select(item => item.Object.queueCode)
                    .Where(queueCode => queueCode != null)
                    .Select(queueCode =>
                    {
                        int.TryParse(queueCode.ToString(), out int result);
                        return result;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                int nextQueueCode;

                // Check if we have any items for today
                if (maxQueueCode == 0)
                {
                    nextQueueCode = 1;
                }
                else
                {
                    nextQueueCode = maxQueueCode + 1;
                }

                // Create an object to store in Firebase
                var queueData = new
                {
                    queueCode = nextQueueCode,
                    dateCode = todayDateCode,
                    timeCreated = DateTime.Now,
                    status = "Pending"
                };

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblQueue")
                    .PostAsync(queueData);

                // Create order object
                var orderData = new
                {
                    orderId = nextQueueCode,
                    dateCode = todayDateCode,
                    items = itemsList
                };

                // Push to Firebase tblOrders
                await _firebaseClient
                    .Child("tblOrders")
                    .PostAsync(orderData);

                // Send SignalR message to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", nextQueueCode + ":" + 1);

                return Ok(nextQueueCode + ":" + 1);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetActiveOrders")]
        public async Task<ActionResult<List<ActiveQueueModel>>> GetActiveOrders()
        {
            try
            {
                // Fetch queue and orders
                var allQueue = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                var allActiveOrders = await _firebaseClient
                    .Child("tblOrders")
                    .OnceAsync<dynamic>();

                var result = new List<ActiveOrdersModel>();

                foreach (var q in allQueue)
                {
                    if (q.Object == null) continue;

                    string status = q.Object.status?.ToString();

                    // Skip if Done
                    if (status == "Done") continue;

                    // Find matching order
                    var order = allActiveOrders.FirstOrDefault(x =>
                        x.Object.orderId == q.Object.queueCode
                    );

                    var orderID = order.Object.orderId;
                    var itemsList = new List<OrderItemModel>();
                    itemsList = ((JArray)order.Object.items).ToObject<List<OrderItemModel>>();

                    if (order != null)
                    {
                        result.Add(new ActiveOrdersModel
                        {
                            OrderId = orderID,
                            Items = itemsList,
                            Status = q.Object.status
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetKnowledgeBase")]
        public async Task<ActionResult> GetKnowledgeBase()
        {
            try
            {
                var KBList = await GetKBListAsync();
                               
                return Ok(JsonConvert.SerializeObject(KBList));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("AddToKnowledgeBase")]
        public async Task<ActionResult> AddToKnowledgeBase([FromBody] string info)
        {
            try
            {
                var KBList = await GetKBListAsync();

                var maxID = KBList.Count;

                var KBData = new KnowledgeBaseModel
                {
                    ID = maxID + 1,
                    Info = info,
                    DateAdded = DateTime.Now
                };

                // Push the object to Firebase
                await _firebaseClient
                    .Child("knowledgeBase")
                    .PostAsync(KBData);

                return Ok(JsonConvert.SerializeObject("Information has been successfully added!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("GetChatResponse")]
        public async Task<ActionResult> GetChatResponse([FromBody] string message)
        {
            try
            {
                var KBList = await GetKBListAsync();

                var stringKB = JsonConvert.SerializeObject(KBList);

                var AIReply = await GetAIResponse(stringKB, message);

                return Ok(AIReply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("SubmitContactFormRequest")]
        public async Task<ActionResult> SubmitContactFormRequest([FromBody] ContactUsFormModel request)
        {
            try
            {
                // Push to firebase
                await _firebaseClient
                    .Child("ContactFormRequests")
                    .PostAsync(request);

                // Send Email Notif
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Contact Form", $"{request.Email}"));
                email.To.Add(new MailboxAddress("Recipient", "keydevsolutions@gmail.com"));
                email.Subject = "New Contact Form Submission";

                email.Body = new TextPart("plain")
                {
                    Text = $"Name: {request.Name}\nEmail: {request.Email}\nMessage:\n{request.Message}"
                };

                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync("keydevsolutions@gmail.com", "qmhm ngys rqgq smwg");
                    await smtp.SendAsync(email);
                    await smtp.DisconnectAsync(true);
                }

                return Ok("Information has been successfully added!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("PushPointsMatrix")]
        public async Task<ActionResult> PushPointsMatrix()
        {
            try
            {
                List<MIGSPointsMatrixModel> migsPointsMatrix = new List<MIGSPointsMatrixModel>();

                string query = "select * from tblMIGSPointsMatrix";
                migsPointsMatrix = conn.Query<MIGSPointsMatrixModel>(query).ToList();

                // 2. Push the object to Firebase
                await _firebaseClient
                    .Child("MIGSPointsMatrix")
                    .PostAsync(migsPointsMatrix);

                return Ok(JsonConvert.SerializeObject("Data has been pushed!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("PushMIGSCriteria")]
        public async Task<ActionResult> PushMIGSCriteria()
        {
            try
            {
                List<MIGSCriteriaModel> MIGSCriteria = new List<MIGSCriteriaModel>();

                string query = "select * from tblMIGSCriteria";
                MIGSCriteria = conn.Query<MIGSCriteriaModel>(query).ToList();

                // 2. Push the object to Firebase
                await _firebaseClient
                    .Child("MIGSCriteria")
                    .PostAsync(MIGSCriteria);

                return Ok(JsonConvert.SerializeObject("Data has been pushed!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("PushEachCriteria")]
        public async Task<ActionResult> PushEachCriteria()
        {
            try
            {
                //tblMIGSYrsMatrix
                List<MIGSYearsMatrixModel> MIGSYrsMatrix = new List<MIGSYearsMatrixModel>();

                string query = "select * from tblMIGSYrsMatrix";
                MIGSYrsMatrix = conn.Query<MIGSYearsMatrixModel>(query).ToList();

                await _firebaseClient
                .Child("MIGSYrsMatrix")
                .PostAsync(MIGSYrsMatrix);


                //tblMIGSShareCapMatrix
                List<MIGSShareCapMatrixModel> MIGSShareCapMatrix = new List<MIGSShareCapMatrixModel>();

                string query2 = "select * from tblMIGSShareCapMatrix";
                MIGSShareCapMatrix = conn.Query<MIGSShareCapMatrixModel>(query2).ToList();

                await _firebaseClient
                    .Child("MIGSShareCapMatrix")
                    .PostAsync(MIGSShareCapMatrix);


                //tblMIGSADBMatrix
                List<MIGSADBMatrixModel> MIGSADBMatrix = new List<MIGSADBMatrixModel>();

                string query3 = "select * from tblMIGSADBMatrix";
                MIGSADBMatrix = conn.Query<MIGSADBMatrixModel>(query3).ToList();

                await _firebaseClient
                    .Child("MIGSADBMatrix")
                    .PostAsync(MIGSADBMatrix);


                //tblMIGSFixedShareCapMatrix
                List<MIGSFixedShareCapMatrixModel> MIGSFixedShareCapMatrix = new List<MIGSFixedShareCapMatrixModel>();

                string query4 = "select * from tblMIGSFixedShareCapMatrix";
                MIGSFixedShareCapMatrix = conn.Query<MIGSFixedShareCapMatrixModel>(query4).ToList();

                await _firebaseClient
                    .Child("MIGSFixedShareCapMatrix")
                    .PostAsync(MIGSFixedShareCapMatrix);


                //tblMIGSPast3Loans
                List<MIGSPast3LoansModel> MIGSPast3Loans = new List<MIGSPast3LoansModel>();

                string query5 = "select * from tblMIGSPast3Loans";
                MIGSPast3Loans = conn.Query<MIGSPast3LoansModel>(query5).ToList();

                await _firebaseClient
                    .Child("MIGSPast3Loans")
                    .PostAsync(MIGSPast3Loans);


                //tblMIGSRecentLoan
                List<MIGSRecentLoanModel> MIGSRecentLoan = new List<MIGSRecentLoanModel>();

                string query6 = "select * from tblMIGSRecentLoan";
                MIGSRecentLoan = conn.Query<MIGSRecentLoanModel>(query6).ToList();

                await _firebaseClient
                    .Child("MIGSRecentLoan")
                    .PostAsync(MIGSRecentLoan);



                return Ok(JsonConvert.SerializeObject("All criteria have been pushed!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("PushMIGSDataToDB")]
        public async Task<ActionResult> PushMIGSDataToDB()
        {
            try
            {
                var migsList = GetMIGSDataFromDatabase();

                // 2. Push the object to Firebase
                await _firebaseClient
                    .Child("MIGS")
                    .PostAsync(migsList);

                return Ok(JsonConvert.SerializeObject("Data has been pushed!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost("GetMIGSQueryResponse")]
        public async Task<ActionResult> GetMIGSQueryResponse([FromBody] int clientID)
        {
            try
            {
                var allItems = await _firebaseClient
            .Child("MIGS")
            .OnceAsync<dynamic>();

                var MemberInfo = new MIGSFullDataModel();
                bool found = false;

                foreach (var item in allItems)
                {
                    if (item.Object == null) continue;

                    var jsonString = item.Object.ToString();
                    var list = JsonConvert.DeserializeObject<List<MIGSFullDataModel>>(jsonString);

                    var FinalList = new List<MIGSFullDataModel>(); 

                    FinalList.AddRange(list);

                    if (list == null) continue;

                    var match = FinalList.FirstOrDefault(x => x.ClientID == clientID);
                    if (match != null)
                    {
                        MemberInfo = match;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return NotFound($"No MIGS data found for client ID: {clientID}");
                }

                string migsJSon = JsonConvert.SerializeObject(MemberInfo);
                var AIReply = await GetAIResponseForMIGSQuery(migsJSon);

                var test = new MemberMIGSDataModel();

                test = JsonConvert.DeserializeObject<MemberMIGSDataModel>(AIReply);

                return Ok(AIReply);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }



        public List<MIGSDataModel> GetMIGSDataFromDatabase()
        {
            try
            {
                List<MIGSDataModel> migsDataList = new List<MIGSDataModel>();

                string query = "select * from MIGSData";
                migsDataList = conn.Query<MIGSDataModel>(query).ToList();

                return migsDataList;
            }
            catch
            {
                return null;
            }
        }



        private async Task<List<KnowledgeBaseModel>> GetKBListAsync()
        {
            // First, get all items and filter locally (for debugging)
            var allItems = await _firebaseClient
                .Child("knowledgeBase")
                .OnceAsync<dynamic>();

            var KBList = new List<KnowledgeBaseModel>();

            foreach (var item in allItems)
            {
                if (item.Object == null) continue;

                var data = new KnowledgeBaseModel
                {
                    ID = item.Object.ID,
                    Info = item.Object.Info,
                    DateAdded = item.Object.DateAdded,
                };

                KBList.Add(data);
            }

            return KBList;
        }

        private async Task<string> GetAIResponse(string KB, string prompt)
        {
            try
            {
                var client = _httpClient;

                string geminiUrl = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");


                var jsonPayload = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"Please create an appropriate chat response based on this knowdledge base:."
                                        + Environment.NewLine
                                        + $"user's new prompt: {KB}."
                                        + Environment.NewLine
                                        + $"This is the chat message:."
                                        + Environment.NewLine
                                        + $"{prompt}"
                            }
                        }
                    }
                }
                };

                var jsonString = JsonConvert.SerializeObject(jsonPayload);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");



                var response = await _httpClient.PostAsync(geminiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<GeminiResponseModel>(responseBody);
                    var text = result.Candidates.FirstOrDefault()?
                                       .Content.Parts.FirstOrDefault()?
                                       .Text;

                    return text;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }



        private async Task<string> GetAIResponseForMIGSQuery(string memberData)
        {
            try
            {
                var client = _httpClient;

                string geminiUrl = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

                // Gen Info
                var MIGSCriteriaKB = await MIGSCriteriaKBAsync();
                var MIGSPointsMatrixKB = await MIGSPointsMatrixKBAsync();

                // Criteria
                var MIGSYrsMatrixKB = await MIGSYrsMatrixKBAsync();
                var MIGSShareCapMatrixKB = await MIGSShareCapMatrixKBAsync();
                var MIGSFixedShareCapMatrixKB = await MIGSFixedShareCapMatrixKBAsync();
                var MIGSADBMatrixKB = await MIGSADBMatrixKBAsync();
                var MIGSPast3LoansKB = await MIGSPast3LoansKBAsync();
                var MIGSRecentLoanKB = await MIGSRecentLoanKBAsync();

                var format = new MemberMIGSDataModel();
                var formatJson = JsonConvert.SerializeObject(format);

                var jsonPayload = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"Please create an appropriate chat response based on this data:"
                                        + Environment.NewLine
                                        + $"Criteria: {MIGSCriteriaKB}."
                                        + Environment.NewLine
                                        + $"PointsMatrix: {MIGSPointsMatrixKB}"
                                        + Environment.NewLine
                                        + $"MIGSYrsMatrixKB: {MIGSYrsMatrixKB}"
                                        + Environment.NewLine
                                        + $"MIGSShareCapMatrixKB: {MIGSShareCapMatrixKB}."
                                        + Environment.NewLine
                                        + $"MIGSFixedShareCapMatrixKB: {MIGSFixedShareCapMatrixKB}"
                                        + Environment.NewLine
                                        + $"MIGSADBMatrixKB: {MIGSADBMatrixKB}"
                                        + Environment.NewLine
                                        + $"MIGSPast3LoansKB: {MIGSPast3LoansKB}"
                                        + Environment.NewLine
                                        + $"MIGSRecentLoanKB: {MIGSRecentLoanKB}"
                                        + Environment.NewLine
                                        + $"user's info: {memberData}."
                                        + Environment.NewLine
                                        + $"make sure your response is in this json format: {formatJson}."
                                        + Environment.NewLine
                                        + $"make sure not to include the ```json at the beginning and ``` at the end of your response."
                                        + Environment.NewLine
                                        + $"Just return the plain json string please with no other comment."
                            }
                        }
                    }
                }
                };

                var jsonString = JsonConvert.SerializeObject(jsonPayload);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");



                var response = await _httpClient.PostAsync(geminiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<GeminiResponseModel>(responseBody);
                    var text = result.Candidates.FirstOrDefault()?
                                       .Content.Parts.FirstOrDefault()?
                                       .Text;

                    return text;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        private async Task<string> MIGSCriteriaKBAsync()
        {
            try
            {
                // MIGSCriteria
                var allItems = await _firebaseClient
                    .Child("MIGSCriteria")
                    .OnceAsync<dynamic>();

                var MIGSCriteriaList = new List<MIGSCriteriaModel>();

                foreach (var item in allItems)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSCriteriaModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSCriteriaList.AddRange(dataList);
                }

                var MIGSCriteriaJson = JsonConvert.SerializeObject(MIGSCriteriaList);

                return MIGSCriteriaJson;

            } catch(Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSPointsMatrixKBAsync()
        {
            try
            {
                // MIGSPointsMatrix
                var allItems2 = await _firebaseClient
                    .Child("MIGSPointsMatrix")
                    .OnceAsync<dynamic>();

                var MIGSPointsMatrixList = new List<MIGSPointsMatrixModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSPointsMatrixModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSPointsMatrixList.AddRange(dataList);
                }

                var MIGSPointsMatrixJson = JsonConvert.SerializeObject(MIGSPointsMatrixList);

                return MIGSPointsMatrixJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSYrsMatrixKBAsync()
        {
            try
            {
                // MIGSYearsMatrix
                var allItems2 = await _firebaseClient
                    .Child("MIGSYearsMatrix")
                    .OnceAsync<dynamic>();

                var MIGSYearsMatrixList = new List<MIGSYearsMatrixModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSYearsMatrixModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSYearsMatrixList.AddRange(dataList);
                }

                var MIGSYearsMatrixJson = JsonConvert.SerializeObject(MIGSYearsMatrixList);

                return MIGSYearsMatrixJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSShareCapMatrixKBAsync()
        {
            try
            {
                // MIGSShareCapMatrix
                var allItems2 = await _firebaseClient
                    .Child("MIGSShareCapMatrix")
                    .OnceAsync<dynamic>();

                var MIGSShareCapMatrixList = new List<MIGSShareCapMatrixModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSShareCapMatrixModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSShareCapMatrixList.AddRange(dataList);
                }

                var MIGSShareCapMatrixJson = JsonConvert.SerializeObject(MIGSShareCapMatrixList);

                return MIGSShareCapMatrixJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSFixedShareCapMatrixKBAsync()
        {
            try
            {
                // MIGSFixedShareCapMatrix
                var allItems2 = await _firebaseClient
                    .Child("MIGSFixedShareCapMatrix")
                    .OnceAsync<dynamic>();

                var MIGSFixedShareCapMatrixList = new List<MIGSFixedShareCapMatrixModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSFixedShareCapMatrixModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSFixedShareCapMatrixList.AddRange(dataList);
                }

                var MIGSFixedShareCapMatrixJson = JsonConvert.SerializeObject(MIGSFixedShareCapMatrixList);

                return MIGSFixedShareCapMatrixJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSADBMatrixKBAsync()
        {
            try
            {
                // MIGSADBMatrix
                var allItems2 = await _firebaseClient
                    .Child("MIGSADBMatrix")
                    .OnceAsync<dynamic>();

                var MIGSADBMatrixList = new List<MIGSADBMatrixModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSADBMatrixModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSADBMatrixList.AddRange(dataList);
                }

                var MIGSADBMatrixJson = JsonConvert.SerializeObject(MIGSADBMatrixList);

                return MIGSADBMatrixJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSPast3LoansKBAsync()
        {
            try
            {
                // MIGSPast3Loans
                var allItems2 = await _firebaseClient
                    .Child("MIGSPast3Loans")
                    .OnceAsync<dynamic>();

                var MIGSPast3LoansList = new List<MIGSPast3LoansModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSPast3LoansModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSPast3LoansList.AddRange(dataList);
                }

                var MIGSPast3LoansJson = JsonConvert.SerializeObject(MIGSPast3LoansList);

                return MIGSPast3LoansJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private async Task<string> MIGSRecentLoanKBAsync()
        {
            try
            {
                // MIGSRecentLoan
                var allItems2 = await _firebaseClient
                    .Child("MIGSRecentLoan")
                    .OnceAsync<dynamic>();

                var MIGSRecentLoanList = new List<MIGSRecentLoanModel>();

                foreach (var item in allItems2)
                {
                    if (item.Object == null) continue;

                    var dataList = JsonConvert.DeserializeObject<List<MIGSRecentLoanModel>>(
                        JsonConvert.SerializeObject(item.Object));

                    MIGSRecentLoanList.AddRange(dataList);
                }

                var MIGSRecentLoanJson = JsonConvert.SerializeObject(MIGSRecentLoanList);

                return MIGSRecentLoanJson;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
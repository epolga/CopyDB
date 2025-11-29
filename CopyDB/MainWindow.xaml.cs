using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using MySqlX.XDevAPI;
using System.ComponentModel.DataAnnotations;
using System.Reflection.PortableExecutable;

namespace CopyDB
{
    public class someClass
    {

        public virtual string NameOfClass { get; set; } = "someClass";
        public string NameOfClassF()
        { return "someClassF"; }
    }
    public class someClass2 : someClass
    {
        public override string NameOfClass { get; set; } = "someClass2";
        public string NameOfClassF()
        { return "someClassF2"; }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        static string connectionMySqlString = "server=cross-stitch-db.cmnauage0ofk.us-east-1.rds.amazonaws.com;" +
                                 "port=3306;" +
                                 "user id=admin;" +
                                 "password=cuerYucHartk;" +
                                 "database=cross_stitch;";
        static string connectionMySqlString1 = "server=cross-stitch-db.cmnauage0ofk.us-east-1.rds.amazonaws.com;" +
                                   "port=3306;" +
                                   "user id=admin;" +
                                   "password=cuerYucHartk;" +
                                   "database=cross-stitch-db;";
        static string connectionMsSqlString = "Data Source=tcp:esql2k803.discountasp.net;Initial Catalog=SQL2008R2_961284_crossstitch;User ID=SQL2008R2_961284_crossstitch_user;Password=Palata#6;TrustServerCertificate=true;";

        // DynamoDB settings
        private static readonly string tableName = "CrossStitchItems";
        private static readonly AmazonDynamoDBClient dynamoClient =
            new AmazonDynamoDBClient(Amazon.RegionEndpoint.USEast1); // Assumes AWS credentials configured
        public MainWindow()
        {
            int _value = 4;

            //int x = (int)Interlocked.Read(location: _value);

            InitializeComponent();
        }
        private async void Copy_Click(object sender, RoutedEventArgs e)
        {
            string str1 = 5.ToString("00000");
            string str2 = 5.ToString("#####");
            // DeleteAll_Designs();
            await DeleteAllItems();
            // await RemoveNPageForAlbums(dynamoClient);

            await Copy_Designs_To_AWSAsync();
            await Copy_Albums_To_AWS();
            await Copy_Designs_To_OrderDbAsync();
            //await ExportDesignsWithMismatchedPagesAsync(@"D:\ann\Git\CopyDB\MismatchedDesigns1.csv");
            //await SyncDesignPagesFromNewAsync();
            //Copy_Designs();
            //Copy_Albums();
        }

        private async void Copy_Users_Click(object sender, RoutedEventArgs e)
        {
            await TrimIds();
            /*
            await DeleteUserItems();
            //await DeleteDeletedUserItems();
            await Copy_Users_To_AWSAsync();*/
        }

        private async Task RemoveNPageForAlbums(AmazonDynamoDBClient client)
        {
            string tableName = "CrossStitchItems";
            string indexName = "AlbumIndex";
            string entityType = "album";
            string pkPrefix = "ALB#";
            string updateExpression = "REMOVE NPage";

            // Step 1: Scan for items with PK starting with "ALB#"
            var scanRequest = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "begins_with(PK, :pkPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pkPrefix", new AttributeValue { S = pkPrefix } }
            },
                ProjectionExpression = "PK"
            };

            Dictionary<string, AttributeValue> scanExclusiveStartKey = null;
            var albumPKs = new HashSet<string>();

            do
            {
                if (scanExclusiveStartKey != null)
                {
                    scanRequest.ExclusiveStartKey = scanExclusiveStartKey;
                }

                var scanResponse = await client.ScanAsync(scanRequest);

                foreach (var item in scanResponse.Items)
                {
                    if (item.TryGetValue("PK", out var pkValue) && pkValue.S != null)
                    {
                        albumPKs.Add(pkValue.S);
                    }
                }

                scanExclusiveStartKey = scanResponse.LastEvaluatedKey;
            } while (scanExclusiveStartKey != null && scanExclusiveStartKey.Count > 0);

            // Step 2: Query LSI for each PK to find items with EntityType = "album"
            foreach (var pk in albumPKs)
            {
                var queryRequest = new QueryRequest
                {
                    TableName = tableName,
                    IndexName = indexName,
                    KeyConditionExpression = "PK = :pkVal AND EntityType = :entityType",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":pkVal", new AttributeValue { S = pk } },
                    { ":entityType", new AttributeValue { S = entityType } }
                }
                };

                Dictionary<string, AttributeValue> queryExclusiveStartKey = null;

                do
                {
                    if (queryExclusiveStartKey != null)
                    {
                        queryRequest.ExclusiveStartKey = queryExclusiveStartKey;
                    }

                    var queryResponse = await client.QueryAsync(queryRequest);

                    foreach (var item in queryResponse.Items)
                    {
                        if (item.TryGetValue("PK", out var pkAttr) && item.TryGetValue("META", out var metaAttr))
                        {
                            var key = new Dictionary<string, AttributeValue>
                        {
                            { "PK", pkAttr },
                            { "META", metaAttr }
                        };

                            var updateRequest = new UpdateItemRequest
                            {
                                TableName = tableName,
                                Key = key,
                                UpdateExpression = updateExpression
                            };

                            await client.UpdateItemAsync(updateRequest);
                        }
                    }

                    queryExclusiveStartKey = queryResponse.LastEvaluatedKey;
                } while (queryExclusiveStartKey != null && queryExclusiveStartKey.Count > 0);
            }
        }


        public static int DeleteAll_Designs()
        {

            int rowsAffected = 0;
            string query = "DELETE FROM designs";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionMySqlString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting Designs: " + ex.Message);
            }

            return rowsAffected;
        }

        public static int Count_Designs()
        {
            int count = 0;
            string query = "SELECT COUNT(*) FROM designs";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionMsSqlString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // ExecuteScalar returns the first column of the first row
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error counting Designs: " + ex.Message);
            }

            return count;
        }

        public static int Count_Users()
        {
            int count = 0;
            string query = "SELECT COUNT(*) FROM AspNetUsers WHERE Deleted = 0 AND PayerID IS NOT NULL";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionMsSqlString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // ExecuteScalar returns the first column of the first row
                        count = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error counting Designs: " + ex.Message);
            }

            return count;
        }

        private async Task SetUsernames()
        {
            var client = new AmazonDynamoDBClient(); // Assumes default credentials and region
            string tableName = "CrossStitchItems";
            string prefix = "USR#";

            // Scan for items where ID begins with "USR#"
            var scanRequest = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "begins_with(#id, :prefix)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#id", "ID" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":prefix", new AttributeValue { S = prefix } }
                }
            };

            List<Dictionary<string, AttributeValue>> items = new List<Dictionary<string, AttributeValue>>();
            ScanResponse scanResponse;

            do
            {
                scanResponse = await client.ScanAsync(scanRequest);
                items.AddRange(scanResponse.Items);
                scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
            } while (scanResponse.LastEvaluatedKey != null && scanResponse.LastEvaluatedKey.Count > 0);

            int i = 0;
            int totalItems = items.Count;
            // Process each item
            foreach (var item in items)
            {
                i++;
                int percent = (int)((i / (totalItems / 100.0)) + 0.5);
                if (!item.ContainsKey("ID") || !item.ContainsKey("NPage"))
                {
                    Console.WriteLine("Item missing required keys; skipping.");
                    continue;
                }

                string id = item["ID"].S;
                string nPage = item["NPage"].S;
                string suffix = id.Substring(prefix.Length); // Extract the rest after "USR#"

                // Define the composite key
                var key = new Dictionary<string, AttributeValue>
                {
                    { "ID", new AttributeValue { S = id } },
                    { "NPage", new AttributeValue { S = nPage } }
                };

                // Update the item with Email and UserName
                var updateRequest = new UpdateItemRequest
                {
                    TableName = tableName,
                    Key = key,
                    UpdateExpression = "SET Email = :email, UserName = :username",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":email", new AttributeValue { S = suffix } },
                        { ":username", new AttributeValue { S = suffix } }
                    }
                };

                try
                {
                    await client.UpdateItemAsync(updateRequest);
                    Console.WriteLine($"Updated item with ID: {id}, NPage: {nPage}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating item {id}: {ex.Message}");
                }
            }

            Console.WriteLine("Processing complete.");


        }

        private async Task TrimIds()
        {
            var client = new AmazonDynamoDBClient(); // Assumes default credentials and region
            string tableName = "CrossStitchItems";
            string prefix = "USR#";

            // Scan for items where ID begins with "USR#"
            var scanRequest = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "begins_with(#id, :prefix)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#id", "ID" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":prefix", new AttributeValue { S = prefix } }
                }
            };

            List<Dictionary<string, AttributeValue>> items = new List<Dictionary<string, AttributeValue>>();
            ScanResponse scanResponse;

            do
            {
                scanResponse = await client.ScanAsync(scanRequest);
                items.AddRange(scanResponse.Items);
                scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
            } while (scanResponse.LastEvaluatedKey != null && scanResponse.LastEvaluatedKey.Count > 0);

            int i = 0;
            int totalItems = items.Count;
            // Process each item
            foreach (var originalItem in items)
            {
                i++;
                int percent = (int)((i / (totalItems / 100.0)) + 0.5);
                if (!originalItem.ContainsKey("ID") || !originalItem.ContainsKey("NPage"))
                {
                    Console.WriteLine("Item missing required keys; skipping.");
                    continue;
                }

                string oldId = originalItem["ID"].S;
                string nPage = originalItem["NPage"].S;
                string newId = oldId.Trim(); // Trim the "USR#" prefix
                if (newId == oldId)
                {
                    continue;
                }
                // Create a new item with the trimmed ID
                var newItem = new Dictionary<string, AttributeValue>(originalItem);
                newItem["ID"] = new AttributeValue { S = newId };

                // Put the new item
                var putRequest = new PutItemRequest
                {
                    TableName = tableName,
                    Item = newItem
                };

                try
                {
                    await client.PutItemAsync(putRequest);
                    Console.WriteLine($"Inserted new item with ID: {newId}, NPage: {nPage}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting new item for original ID {oldId}: {ex.Message}");
                    continue; // Skip deletion if insertion fails
                }

                if (oldId != newId)
                {
                    // Delete the old item
                    var deleteKey = new Dictionary<string, AttributeValue>
                    {
                        { "ID", new AttributeValue { S = oldId } },
                        { "NPage", new AttributeValue { S = nPage } }
                    };
                    var deleteRequest = new DeleteItemRequest
                    {
                        TableName = tableName,
                        Key = deleteKey
                    };

                    try
                    {
                        await client.DeleteItemAsync(deleteRequest);
                        Console.WriteLine($"Deleted old item with ID: {oldId}, NPage: {nPage}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting old item {oldId}: {ex.Message}");
                    }
                }
                else
                {

                }
            }

            Console.WriteLine("Processing complete.");

        }
        private async Task Copy_Users_To_AWSAsync()
        {
            // SQL query to select all records from the MSSQL Albums table.
            string selectQuery = "SELECT * FROM AspNetUsers WHERE Deleted = 0 AND PayerID IS NOT NULL ORDER BY DateCreated";
            int nUsers = Count_Users();
            int nUser = 0;
            var table = Table.LoadTable(dynamoClient, tableName);
            try
            {
                // Open connection to MSSQL database
                using (SqlConnection sqlConn = new SqlConnection(connectionMsSqlString))
                {
                    sqlConn.Open();
                    using (SqlCommand sqlCmd = new SqlCommand(selectQuery, sqlConn))
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        double usersPercent = 0;
                        while (await reader.ReadAsync())
                        {
                            nUser++;
                            usersPercent = nUser / (nUsers / 100.0);
                            int percent = (int)(usersPercent + 0.5);
                            string NPage = nUser.ToString("0000000");
                            string ID = reader.GetString("ID");
                            string FName = reader.IsDBNull(reader.GetOrdinal("FName")) ? "SomeName" : reader.GetString("FName");
                            string UserName = reader.IsDBNull(reader.GetOrdinal("Email")) ? "SomeEmail" : reader.GetString("Email");
                            string Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? "SomeEmail" : reader.GetString("Email");
                            DateTime DateCreated = reader.IsDBNull(reader.GetOrdinal("DateCreated")) ? DateTime.MinValue : reader.GetDateTime("DateCreated");
                            DateTime LastLoginDate = reader.IsDBNull(reader.GetOrdinal("LastLoginDate")) ? DateTime.MinValue : reader.GetDateTime("LastLoginDate");
                            DateTime? PayingDate = reader.IsDBNull(reader.GetOrdinal("PayingDate")) ? null : reader.GetDateTime("PayingDate");
                            decimal PayedAmount = reader.IsDBNull(reader.GetOrdinal("PayedAmount")) ? 0 : reader.GetDecimal("PayedAmount");
                            bool IsRecurring = reader.IsDBNull(reader.GetOrdinal("IsRecurring")) ? false : reader.GetBoolean("IsRecurring");
                            string PayerID = reader.IsDBNull(reader.GetOrdinal("PayerID")) ? "" : reader.GetString("PayerID");
                            bool Deleted = reader.IsDBNull(reader.GetOrdinal("Deleted")) ? false : reader.GetBoolean("Deleted");
                            int OldEmailID = reader.IsDBNull(reader.GetOrdinal("OldEmailID")) ? 0 : reader.GetInt32("OldEmailID");
                            string IP = reader.IsDBNull(reader.GetOrdinal("IP")) ? "" : reader.GetString("IP");
                            DateTime? FirstPayingDate = reader.IsDBNull(reader.GetOrdinal("FirstPayingDate")) ? null : reader.GetDateTime("FirstPayingDate");
                            string OpenPwd = reader.IsDBNull(reader.GetOrdinal("OpenPwd")) ? "" : reader.GetString("OpenPwd");

                            if (!string.IsNullOrEmpty(PayerID))
                            {
                            }
                            var item = new Document
                            {
                                ["ID"] = $"USR#{Email}",
                                ["NPage"] = NPage,
                                ["EntityType"] = "USER",
                                ["FName"] = FName,
                                ["UserName"] = UserName,
                                ["DateCreated"] = DateCreated,
                                ["LastLoginDate"] = LastLoginDate,
                                ["PayingDate"] = PayingDate,
                                ["PayedAmount"] = PayedAmount,
                                ["IsRecurring"] = IsRecurring,
                                ["PayerID"] = PayerID,
                                ["Deleted"] = Deleted,
                                ["OldEmailID"] = OldEmailID,
                                ["IP"] = IP,
                                ["FirstPayingDate"] = FirstPayingDate,
                                ["OpenPwd"] = OpenPwd
                            };
                            await table.PutItemAsync(item);
                            Console.WriteLine($"Uploaded user {nUser}: USR#{ID}");

                        }

                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async Task Copy_Albums_To_AWS()
        {
            // SQL query to select all records from the MSSQL Albums table.
            string selectQuery = "SELECT AlbumID, Caption FROM Albums ORDER BY AlbumID";

            try
            {
                // Open connection to MSSQL database
                using (SqlConnection sqlConn = new SqlConnection(connectionMsSqlString))
                {
                    sqlConn.Open();
                    using (SqlCommand sqlCmd = new SqlCommand(selectQuery, sqlConn))
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        int albumCount = 0;
                        while (await reader.ReadAsync())
                        {
                            /* if(albumCount > 3)
                             {
                                 break;
                             }*/
                            try
                            {
                                int albumId = reader.GetInt32("AlbumID");
                                string caption = reader.IsDBNull(reader.GetOrdinal("Caption")) ? $"Album {reader.GetInt32("AlbumID")}" : reader.GetString("Caption");
                                var item = new Document
                                {
                                    ["ID"] = $"ALB#{albumId.ToString("0000")}",
                                    ["NPage"] = "00000",
                                    ["EntityType"] = "ALBUM",
                                    ["AlbumID"] = albumId,
                                    ["Caption"] = caption,
                                    // ["NPage"] = albumCount
                                };
                                /*
                                var item = new Dictionary<string, AttributeValue>
                                {
                                    { "PK", new AttributeValue { S = $"ALB#{reader.GetInt32("AlbumID")}" } },
                                    { "SK", new AttributeValue { S = "DATA" } },
                                    { "AlbumID", new AttributeValue { N = reader.GetInt32("AlbumID").ToString() } },
                                    { "Caption", new AttributeValue { S = reader.IsDBNull(reader.GetOrdinal("Caption")) ? $"Album {reader.GetInt32("AlbumID")}" : reader.GetString("Caption") } }
                                };
                                */
                                var table = Table.LoadTable(dynamoClient, tableName);
                                await table.PutItemAsync(item);
                                albumCount++;
                                Console.WriteLine($"Uploaded album {albumCount}: ALB#{reader.GetInt32("AlbumID")}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error uploading album ID {reader.GetInt32("AlbumID")}: {ex.Message}");
                            }
                        }

                        Console.WriteLine($"Total albums uploaded: {albumCount}");

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static async Task DeleteAllItems()
        {
            int totalDeleted = 0;
            Dictionary<string, AttributeValue> lastEvaluatedKey = null;

            do
            {
                // Scan table to get items
                var scanRequest = new ScanRequest
                {
                    TableName = tableName,
                    AttributesToGet = new List<string> { "ID", "NPage" }, // Only need keys for deletion
                    ExclusiveStartKey = lastEvaluatedKey,
                    Limit = 100 // Adjust for performance (100 items per scan)
                };

                var scanResponse = await dynamoClient.ScanAsync(scanRequest);
                var items = scanResponse.Items;
                lastEvaluatedKey = scanResponse.LastEvaluatedKey.Count > 0 ? scanResponse.LastEvaluatedKey : null;

                if (items.Count == 0)
                {
                    Console.WriteLine("No items found in scan batch.");
                    continue;
                }

                // Process items in batches of 25 (DynamoDB BatchWriteItem limit)
                var batches = items
                    .Select((item, index) => new { Item = item, Index = index })
                    .GroupBy(x => x.Index / 25)
                    .Select(g => g.Select(x => x.Item).ToList());

                foreach (var batch in batches)
                {
                    var deleteRequests = batch.Select(item => new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>
                        {
                            { "ID", item["ID"] },
                            { "NPage", item["NPage"] }
                        }
                        }
                    }).ToList();

                    var batchWriteRequest = new BatchWriteItemRequest
                    {
                        RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        { tableName, deleteRequests }
                    }
                    };

                    try
                    {
                        await dynamoClient.BatchWriteItemAsync(batchWriteRequest);
                        totalDeleted += deleteRequests.Count;
                        Console.WriteLine($"Deleted batch of {deleteRequests.Count} items. Total deleted: {totalDeleted}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting batch: {ex.Message}");
                        // Log specific keys for debugging
                        foreach (var req in deleteRequests)
                        {
                            var pk = req.DeleteRequest.Key["PK"].S;
                            var sk = req.DeleteRequest.Key["SK"].S;
                            Console.WriteLine($"Failed item: PK={pk}, SK={sk}");
                        }
                    }
                }

            } while (lastEvaluatedKey != null);

            Console.WriteLine($"Total items deleted: {totalDeleted}");
        }
        private void Copy_Albums()
        {
            // SQL query to select all records from the MSSQL Albums table.
            string selectQuery = "SELECT AlbumID, Caption FROM Albums";

            try
            {
                // Open connection to MSSQL database
                using (SqlConnection sqlConn = new SqlConnection(connectionMsSqlString))
                {
                    sqlConn.Open();
                    using (SqlCommand sqlCmd = new SqlCommand(selectQuery, sqlConn))
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        // Open connection to MySQL database
                        using (MySqlConnection mySqlConn = new MySqlConnection(connectionMySqlString))
                        {
                            mySqlConn.Open();

                            // SQL command for inserting a record into the MySQL Albums table.
                            string insertQuery = "INSERT INTO albums (AlbumID, Caption) VALUES (@AlbumID, @Caption)";
                            using (MySqlCommand mySqlCmd = new MySqlCommand(insertQuery, mySqlConn))
                            {
                                // Define parameters
                                mySqlCmd.Parameters.Add("@AlbumID", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@Caption", MySqlDbType.VarChar, 50);

                                // Loop through each record from MSSQL and insert into MySQL.
                                while (reader.Read())
                                {
                                    mySqlCmd.Parameters["@AlbumID"].Value = reader["AlbumID"];
                                    mySqlCmd.Parameters["@Caption"].Value = reader["Caption"];

                                    // Execute the insert command for each record.
                                    mySqlCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Data copied successfully from MSSQL Albums to MySQL Albums.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static async Task DeleteDeletedUserItems()
        {
            int totalDeleted = 0;
            Dictionary<string, AttributeValue> lastEvaluatedKey = null;

            do
            {
                // Scan table to get items with EntityType = "USER"
                var scanRequest = new ScanRequest
                {
                    TableName = tableName,
                    ProjectionExpression = "#id, #nPage", // Use ProjectionExpression instead of AttributesToGet
                    ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#id", "ID" }, // Alias for reserved keyword ID
                    { "#nPage", "NPage" } // Alias for NPage
                },
                    FilterExpression = "EntityType = :entityType",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":entityType", new AttributeValue { S = "USER" } }
                },
                    ExclusiveStartKey = lastEvaluatedKey,
                    Limit = 100 // Adjust for performance (100 items per scan)
                };

                var scanResponse = await dynamoClient.ScanAsync(scanRequest);
                var items = scanResponse.Items;
                lastEvaluatedKey = scanResponse.LastEvaluatedKey.Count > 0 ? scanResponse.LastEvaluatedKey : null;

                if (items.Count == 0)
                {
                    Console.WriteLine("No USER items found in scan batch.");
                    continue;
                }

                // Process items in batches of 25 (DynamoDB BatchWriteItem limit)
                var batches = items
                    .Select((item, index) => new { Item = item, Index = index })
                    .GroupBy(x => x.Index / 25)
                    .Select(g => g.Select(x => x.Item).ToList());

                foreach (var batch in batches)
                {
                    var deleteRequests = batch.Select(item => new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>
                        {
                            { "ID", item["ID"] },
                            { "NPage", item["NPage"] }
                        }
                        }
                    }).ToList();

                    var batchWriteRequest = new BatchWriteItemRequest
                    {
                        RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        { tableName, deleteRequests }
                    }
                    };

                    try
                    {
                        await dynamoClient.BatchWriteItemAsync(batchWriteRequest);
                        totalDeleted += deleteRequests.Count;
                        Console.WriteLine($"Deleted batch of {deleteRequests.Count} USER items. Total deleted: {totalDeleted}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting batch: {ex.Message}");
                        // Log specific keys for debugging
                        foreach (var req in deleteRequests)
                        {
                            var id = req.DeleteRequest.Key["ID"].S;
                            var nPage = req.DeleteRequest.Key["NPage"].S;
                            Console.WriteLine($"Failed item: ID={id}, NPage={nPage}");
                        }
                    }
                }

            } while (lastEvaluatedKey != null);

            Console.WriteLine($"Total USER items deleted: {totalDeleted}");
        }

        public static async Task DeleteUserItems()
        {
            int totalDeleted = 0;
            Dictionary<string, AttributeValue> lastEvaluatedKey = null;

            do
            {
                // Scan table to get items with EntityType = "USER"
                var scanRequest = new ScanRequest
                {
                    TableName = tableName,
                    ProjectionExpression = "#id, #nPage", // Use ProjectionExpression instead of AttributesToGet
                    ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#id", "ID" }, // Alias for reserved keyword ID
                    { "#nPage", "NPage" } // Alias for NPage
                },
                    FilterExpression = "EntityType = :entityType",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":entityType", new AttributeValue { S = "USER" } }
                },
                    ExclusiveStartKey = lastEvaluatedKey,
                    Limit = 100 // Adjust for performance (100 items per scan)
                };

                var scanResponse = await dynamoClient.ScanAsync(scanRequest);
                var items = scanResponse.Items;
                lastEvaluatedKey = scanResponse.LastEvaluatedKey.Count > 0 ? scanResponse.LastEvaluatedKey : null;

                if (items.Count == 0)
                {
                    Console.WriteLine("No USER items found in scan batch.");
                    continue;
                }

                // Process items in batches of 25 (DynamoDB BatchWriteItem limit)
                var batches = items
                    .Select((item, index) => new { Item = item, Index = index })
                    .GroupBy(x => x.Index / 25)
                    .Select(g => g.Select(x => x.Item).ToList());

                foreach (var batch in batches)
                {
                    var deleteRequests = batch.Select(item => new WriteRequest
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>
                        {
                            { "ID", item["ID"] },
                            { "NPage", item["NPage"] }
                        }
                        }
                    }).ToList();

                    var batchWriteRequest = new BatchWriteItemRequest
                    {
                        RequestItems = new Dictionary<string, List<WriteRequest>>
                    {
                        { tableName, deleteRequests }
                    }
                    };

                    try
                    {
                        await dynamoClient.BatchWriteItemAsync(batchWriteRequest);
                        totalDeleted += deleteRequests.Count;
                        Console.WriteLine($"Deleted batch of {deleteRequests.Count} USER items. Total deleted: {totalDeleted}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting batch: {ex.Message}");
                        // Log specific keys for debugging
                        foreach (var req in deleteRequests)
                        {
                            var id = req.DeleteRequest.Key["ID"].S;
                            var nPage = req.DeleteRequest.Key["NPage"].S;
                            Console.WriteLine($"Failed item: ID={id}, NPage={nPage}");
                        }
                    }
                }

            } while (lastEvaluatedKey != null);

            Console.WriteLine($"Total USER items deleted: {totalDeleted}");
        }

        private async Task ExportDesignsWithMismatchedPagesAsync(string outputPath)
        {
            try
            {
                var table = Table.LoadTable(dynamoClient, tableName);
                var filter = new ScanFilter();
                filter.AddCondition("EntityType", ScanOperator.Equal, "DESIGN");
                var search = table.Scan(filter);

                var sb = new StringBuilder();
                sb.AppendLine("DesignID,AlbumID,NPage,NewNPage,Caption");

                int mismatched = 0;

                do
                {
                    var batch = await search.GetNextSetAsync();
                    foreach (var doc in batch)
                    {
                        var designId = TryGetInt(doc, "DesignID");
                        var albumId = TryGetInt(doc, "AlbumID");
                        var nPage = TryGetString(doc, "NPage");
                        var newNPage = TryGetString(doc, "NewNPage");

                        if (string.Equals(nPage, newNPage, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var caption = TryGetString(doc, "Caption");
                        sb.AppendLine(string.Join(",",
                            Csv(designId?.ToString() ?? string.Empty),
                            Csv(albumId?.ToString() ?? string.Empty),
                            Csv(nPage),
                            Csv(newNPage),
                            Csv(caption)));
                        mismatched++;
                    }
                } while (!search.IsDone);

                File.WriteAllText(outputPath, sb.ToString());
                Console.WriteLine($"Exported {mismatched} designs with mismatched NPage/NewNPage to {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting mismatched designs: {ex.Message}");
            }

            static int? TryGetInt(Document doc, string key)
            {
                if (!doc.ContainsKey(key))
                {
                    return null;
                }

                var entry = doc[key];
                if (entry is null)
                {
                    return null;
                }

                try
                {
                    return entry.AsInt();
                }
                catch
                {
                }

                try
                {
                    if (int.TryParse(entry.AsString(), out var value))
                    {
                        return value;
                    }
                }
                catch
                {
                }

                return null;
            }

            static string TryGetString(Document doc, string key)
            {
                if (!doc.ContainsKey(key))
                {
                    return string.Empty;
                }

                var entry = doc[key];
                if (entry is null)
                {
                    return string.Empty;
                }

                try
                {
                    return entry.AsString();
                }
                catch
                {
                }

                try
                {
                    return entry.AsInt().ToString();
                }
                catch
                {
                }

                return string.Empty;
            }

            static string Csv(string value)
            {
                value ??= string.Empty;
                var needsQuotes = value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r");
                if (value.Contains("\""))
                {
                    value = value.Replace("\"", "\"\"");
                }

                return needsQuotes ? $"\"{value}\"" : value;
            }
        }

        private async Task SyncDesignPagesFromNewAsync()
        {
            try
            {
                var table = Table.LoadTable(dynamoClient, tableName);
                var filter = new ScanFilter();
                filter.AddCondition("EntityType", ScanOperator.Equal, "DESIGN");
                var search = table.Scan(filter);

                int updated = 0;
                int missingNew = 0;

                do
                {
                    var batch = await search.GetNextSetAsync();
                    foreach (var doc in batch)
                    {
                        var nPage = ReadString(doc, "NPage");
                        var newNPage = ReadString(doc, "NewNPage");

                        if (string.IsNullOrEmpty(newNPage))
                        {
                            missingNew++;
                            continue;
                        }

                        if (string.Equals(nPage, newNPage, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        doc["NPage"] = newNPage;
                        try
                        {
                            await table.UpdateItemAsync(doc);
                            updated++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to update NPage for design {ReadString(doc, "DesignID")}: {ex.Message}");
                        }
                    }
                } while (!search.IsDone);

                Console.WriteLine($"Synced NPage from NewNPage for {updated} designs. Missing NewNPage: {missingNew}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing NPage from NewNPage: {ex.Message}");
            }

            static string ReadString(Document doc, string key)
            {
                if (!doc.ContainsKey(key) || doc[key] is null)
                {
                    return string.Empty;
                }

                var entry = doc[key];
                try
                {
                    return entry.AsString();
                }
                catch
                {
                }

                try
                {
                    return entry.AsInt().ToString("00000");
                }
                catch
                {
                }

                return string.Empty;
            }
        }

        private async Task Copy_Designs_To_OrderDbAsync()
        {
            try
            {
                var table = Table.LoadTable(dynamoClient, tableName);
                var scanFilter = new ScanFilter();
                scanFilter.AddCondition("EntityType", ScanOperator.Equal, "DESIGN");
                var search = table.Scan(scanFilter);
                var designDocs = new List<Document>();

                do
                {
                    var batch = await search.GetNextSetAsync();
                    designDocs.AddRange(batch);
                } while (!search.IsDone);

                var groupedDesigns = designDocs
                    .Select(d => new
                    {
                        Doc = d,
                        AlbumId = TryGetInt(d, "AlbumID"),
                        DesignId = TryGetInt(d, "DesignID"),
                        NPage = TryGetInt(d, "NPage")
                    })
                    .Where(x => x.AlbumId.HasValue && x.DesignId.HasValue)
                    .GroupBy(x => x.AlbumId!.Value);

                int totalUpdated = 0;

                foreach (var albumGroup in groupedDesigns)
                {
                    int newNPage = 1;
                    foreach (var design in albumGroup
                        .OrderBy(d => d.NPage ?? int.MaxValue)
                        .ThenBy(d => d.DesignId.Value))
                    {
                        var doc = design.Doc;
                        doc["NewNPage"] = newNPage.ToString("00000");

                        try
                        {
                            await table.UpdateItemAsync(doc);
                            totalUpdated++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to update design {design.DesignId} (album {albumGroup.Key}) with NewNPage={newNPage}: {ex.Message}");
                        }

                        newNPage++;
                    }
                }

                Console.WriteLine($"Updated {totalUpdated} designs in DynamoDB with NewNPage set sequentially per album.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating designs in DynamoDB with NewNPage: {ex.Message}");
            }

            static int? TryGetInt(Document doc, string key)
            {
                if (!doc.ContainsKey(key))
                {
                    return null;
                }

                var entry = doc[key];
                if (entry is null)
                {
                    return null;
                }

                try
                {
                    return entry.AsInt();
                }
                catch
                {
                }

                try
                {
                    if (int.TryParse(entry.AsString(), out var value))
                    {
                        return value;
                    }
                }
                catch
                {
                }

                return null;
            }

            static string TryGetString(Document doc, string key)
            {
                var value = TryGetNullableString(doc, key);
                return value.ToString() ?? string.Empty;
            }

            static object TryGetNullableString(Document doc, string key)
            {
                if (!doc.ContainsKey(key))
                {
                    return DBNull.Value;
                }

                var entry = doc[key];
                if (entry is null)
                {
                    return DBNull.Value;
                }

                try
                {
                    var str = entry.AsString();
                    if (string.Equals(str, "NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        return DBNull.Value;
                    }

                    return str;
                }
                catch
                {
                    return DBNull.Value;
                }
            }
        }

        private async Task Copy_Designs_To_AWSAsync()
        {
            string selectQuery = @"
                SELECT DesignID, AlbumID, Caption, Description, NDownloaded, 
                       Notes, Width, Height, NColors, Text, NPage 
                FROM Designs ORDER BY DesignID";
            Dictionary<int, int> dictAlbumToDesign = new Dictionary<int, int>();
            var table = Table.LoadTable(dynamoClient, tableName);
            var existingDesignIds = await GetExistingDesignIds(table);

            try
            {
                int nDesigns = 0;
                int curAlbumId = -1;
                int nPage = 0;
                int nGlobalPage = 1;
                // Open MSSQL connection
                using (SqlConnection sqlConn = new SqlConnection(connectionMsSqlString))
                {
                    sqlConn.Open();
                    using (SqlCommand sqlCmd = new SqlCommand(selectQuery, sqlConn))
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        int designCount = 0;
                        while (await reader.ReadAsync())
                        {
                            /*if(designCount > 2)
                            {
                                break;
                            }*/

                            int designId = reader.GetInt32("DesignID");
                            if (existingDesignIds.Contains(designId))
                            {
                                continue;
                            }

                            int albumId = reader.GetInt32("AlbumID");
                            if (dictAlbumToDesign.ContainsKey(albumId))
                            {
                                nPage = dictAlbumToDesign[albumId] + 1;
                                /*if(nPage > 3)
                                  {
                                      break;
                                  }*/
                            }
                            else
                            {
                                nPage = 1;
                            }
                            dictAlbumToDesign[albumId] = nPage;
                            string caption = reader.IsDBNull(reader.GetOrdinal("Caption")) ? $"Design {reader.GetInt32("DesignID")}" : reader.GetString("Caption");
                            string description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description");
                            int nDownloaded = reader.IsDBNull(reader.GetOrdinal("NDownloaded")) ? 0 : reader.GetInt32("NDownloaded");
                            string notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString("Notes");
                            int width = reader.IsDBNull(reader.GetOrdinal("Width")) ? 0 : reader.GetInt32("Width");
                            int height = reader.IsDBNull(reader.GetOrdinal("Height")) ? 0 : reader.GetInt32("Width");
                            string text = reader.IsDBNull(reader.GetOrdinal("Text")) || reader.GetString("Text") == "NULL" ? "" : reader.GetString("Text");
                            int nColors = reader.IsDBNull(reader.GetOrdinal("NColors")) ? 0 : reader.GetInt32("NColors");
                            // int nPage = reader.IsDBNull(reader.GetOrdinal("NPage")) ? 0 : reader.GetInt32("NPage");

                            var item = new Document
                            {
                                ["ID"] = $"ALB#{albumId.ToString("0000")}",
                                ["NPage"] = nPage.ToString("00000"),
                                //["SK"] = designId.ToString("000000000"),
                                ["EntityType"] = "DESIGN",
                                ["DesignID"] = designId,
                                ["AlbumID"] = albumId,
                                ["Caption"] = caption,
                                ["Description"] = description,
                                ["NColors"] = nColors,
                                ["NDownloaded"] = nDownloaded,
                                ["Notes"] = notes,
                                ["Width"] = width,
                                ["Height"] = height,
                                ["Text"] = text,
                                //  ["NPage"] = nPage,
                                ["NGlobalPage"] = nGlobalPage
                            };

                            nGlobalPage++;
                            /*
                            var item = new Dictionary<string, AttributeValue>
                            {
                                { "PK", new AttributeValue { S = $"DSN#{reader.GetInt32("DesignID")}" } },
                                { "SK", new AttributeValue { S = "DATA" } },
                                { "GSI1PK", new AttributeValue { S = "DESIGNS" } },
                                { "GSI1SK", new AttributeValue { S = $"DSN#{reader.GetInt32("DesignID")}" } },
                                { "DesignID", new AttributeValue { N = reader.GetInt32("DesignID").ToString() } },
                                { "AlbumID", new AttributeValue { N = reader.GetInt32("AlbumID").ToString() } },
                                { "Caption", new AttributeValue { S = reader.IsDBNull(reader.GetOrdinal("Caption")) ? $"Design {reader.GetInt32("DesignID")}" : reader.GetString("Caption") } },
                                { "Description", new AttributeValue { S = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description") } },
                                { "NDownloaded", new AttributeValue { N = reader.IsDBNull(reader.GetOrdinal("NDownloaded")) ? "0" : reader.GetInt32("NDownloaded").ToString() } },
                                { "Notes", new AttributeValue { S = reader.IsDBNull(reader.GetOrdinal("Notes")) ? "" : reader.GetString("Notes") } },
                                { "Width", new AttributeValue { N = reader.IsDBNull(reader.GetOrdinal("Width")) ? "0" : reader.GetInt32("Width").ToString() } },
                                { "Height", new AttributeValue { N = reader.IsDBNull(reader.GetOrdinal("Height")) ? "0" : reader.GetInt32("Height").ToString() } },
                                { "Text", new AttributeValue { S = reader.IsDBNull(reader.GetOrdinal("Text")) || reader.GetString("Text") == "NULL" ? "" : reader.GetString("Text") } },
                                { "NPage", new AttributeValue { N = reader.IsDBNull(reader.GetOrdinal("NPage")) ? "0" : reader.GetInt32("NPage").ToString() } }
                            };
                            */
                            /*
                            var request = new PutItemRequest
                            {
                                TableName = tableName,
                                Item = item
                            };
                            await dynamoClient.PutItemAsync(request);
                            */
                            await table.PutItemAsync(item);
                            existingDesignIds.Add(designId);
                            designCount++;
                            Console.WriteLine($"Uploaded design {designCount}: DSN#{reader.GetInt32("DesignID")}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            static async Task<HashSet<int>> GetExistingDesignIds(Table table)
            {
                var designIds = new HashSet<int>();
                var scanFilter = new ScanFilter();
                scanFilter.AddCondition("EntityType", ScanOperator.Equal, "DESIGN");
                var search = table.Scan(scanFilter);

                do
                {
                    var batch = await search.GetNextSetAsync();
                    foreach (var doc in batch)
                    {
                        if (!doc.ContainsKey("DesignID"))
                        {
                            continue;
                        }

                        var entry = doc["DesignID"];
                        if (entry is null)
                        {
                            continue;
                        }

                        try
                        {
                            designIds.Add(entry.AsInt());
                            continue;
                        }
                        catch
                        {
                        }

                        try
                        {
                            if (int.TryParse(entry.AsString(), out var parsed))
                            {
                                designIds.Add(parsed);
                            }
                        }
                        catch
                        {
                        }
                    }
                } while (!search.IsDone);

                return designIds;
            }
        }
        private void Copy_Designs()
        {
            // SQL query to select all records from the MSSQL Designs table.
            string selectQuery = @"
                SELECT DesignID, AlbumID, Caption, Description, NDownloaded, 
                       Notes, Width, Height, NColors, Text, NPage 
                FROM Designs";
            try
            {
                int nDesigns = 0;
                // Open MSSQL connection
                using (SqlConnection sqlConn = new SqlConnection(connectionMsSqlString))
                {
                    sqlConn.Open();
                    using (SqlCommand sqlCmd = new SqlCommand(selectQuery, sqlConn))
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        // Open MySQL connection
                        using (MySqlConnection mySqlConn = new MySqlConnection(connectionMySqlString))
                        {
                            mySqlConn.Open();

                            // SQL for inserting a record into the MySQL Designs table.
                            string insertQuery = @"
                                INSERT INTO designs 
                                    (DesignID, AlbumID, Caption, Description, NDownloaded, 
                                     Notes, Width, Height, NColors, Text, NPage)
                                VALUES
                                    (@DesignID, @AlbumID, @Caption, @Description, @NDownloaded, 
                                     @Notes, @Width, @Height, @NColors, @Text, @NPage)";

                            using (MySqlCommand mySqlCmd = new MySqlCommand(insertQuery, mySqlConn))
                            {
                                // Define parameters
                                mySqlCmd.Parameters.Add("@DesignID", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@AlbumID", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@Caption", MySqlDbType.VarChar, 50);
                                mySqlCmd.Parameters.Add("@Description", MySqlDbType.VarChar, 100);
                                mySqlCmd.Parameters.Add("@NDownloaded", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@Notes", MySqlDbType.VarChar, 500);
                                mySqlCmd.Parameters.Add("@Width", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@Height", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@NColors", MySqlDbType.Int32);
                                mySqlCmd.Parameters.Add("@Text", MySqlDbType.VarChar, 500);
                                mySqlCmd.Parameters.Add("@NPage", MySqlDbType.Int32);

                                // Loop through each record from MSSQL and insert into MySQL.
                                while (reader.Read())
                                {
                                    if (reader["AlbumID"].ToString() == "83")
                                        continue;
                                    if (reader["AlbumID"].ToString() == "46")
                                        continue;
                                    nDesigns++;
                                    mySqlCmd.Parameters["@DesignID"].Value = reader["DesignID"];
                                    mySqlCmd.Parameters["@AlbumID"].Value = reader["AlbumID"];
                                    mySqlCmd.Parameters["@Caption"].Value = reader["Caption"] == DBNull.Value ? (object)DBNull.Value : reader["Caption"];
                                    mySqlCmd.Parameters["@Description"].Value = reader["Description"];
                                    mySqlCmd.Parameters["@NDownloaded"].Value = reader["NDownloaded"];
                                    mySqlCmd.Parameters["@Notes"].Value = reader["Notes"] == DBNull.Value ? (object)DBNull.Value : reader["Notes"];
                                    mySqlCmd.Parameters["@Width"].Value = reader["Width"];
                                    mySqlCmd.Parameters["@Height"].Value = reader["Height"];
                                    mySqlCmd.Parameters["@NColors"].Value = reader["NColors"];
                                    mySqlCmd.Parameters["@Text"].Value = reader["Text"] == DBNull.Value ? (object)DBNull.Value : reader["Text"];
                                    mySqlCmd.Parameters["@NPage"].Value = reader["NPage"] == DBNull.Value ? (object)DBNull.Value : reader["NPage"];

                                    try
                                    {
                                        // Execute the insert command for each record.
                                        mySqlCmd.ExecuteNonQuery();
                                    }
                                    catch
                                    {
                                        nDesigns--;
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("Data copied successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        void Reverse(char[] str)
        {
            if (str == null || str.Length == 0) { return; }

            int length = str.Length;

            int left = 0; ;
            int right = length - 1;
            char tmp = '\0';
            while (left < right)
            {
                tmp = str[left];
                str[left] = str[right];
                str[right] = tmp;
                left++;
                right++;
            }

        }
        bool IsPrime(uint num)
        {
            double dblNum = (double)num;
            uint limit = (uint)Math.Sqrt(dblNum);
            if (limit * limit == num) { return false; }
            for (int number = 2; number < limit; number++)
            {
                if (num % number == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public int[] TwoSum(int[] nums, int target)
        {
            Dictionary<int, int> numMap = new Dictionary<int, int>();
            for (int i = 0; i < nums.Length; i++)
            {
                int complement = target - nums[i];
                if (numMap.ContainsKey(complement))
                {
                    return new int[] { numMap[complement], i };
                }
                numMap[complement] = i;
            }
            // If no solution found (though problem assumes one exists)
            throw new ArgumentException("No two numbers add up to the target.");
        }

        public async Task<int> Sum(int[] nums)
        {
            int sum = 0;
            if (nums == null)
            {
                throw new ArgumentNullException("nums", "nums is null");
            }

            if (nums.Length == 0)
            {
                return 0;
            }
            if (nums.Length == 1)
            {
                return nums[0];
            }
            int middle = nums.Length / 2;

            Tuple<int[], int, int> tuple1 = new Tuple<int[], int, int>(nums, 0, middle);
            Tuple<int[], int, int> tuple2 = new Tuple<int[], int, int>(nums, middle, nums.Length);
            int res1 = 0;
            int res2 = 0;
            Thread thread1 = new Thread(() => Sum(tuple1, ref res1));
            Thread thread2 = new Thread(() => Sum(tuple2, ref res2));
            thread1.Start();
            thread2.Start();
            var task1 = Task.Run(() => Sum(nums, 0, middle));
            var task2 = Task.Run(() => Sum(nums, middle, nums.Length));
            _ = Task.WhenAll(task1, task2);
            int result = task1.Result + task2.Result;
            thread1.Join();
            thread2.Join();
            return res1 + res2;
        }

        public int Sum(int[] ints, int start, int end)
        {
            int sum = 0;
            if (start < 0 || end > ints.Length)
            {
                throw new ArgumentException();
            }
            for (int i = start; i < end; i++)
            {
                sum += ints[i];
            }
            return sum;
        }
        public void Sum(Tuple<int[], int, int> tuple, ref int res)
        {
            int[] ints = tuple.Item1;
            int start = tuple.Item2;
            int end = tuple.Item3;
            int sum = 0;
            if (start < 0 || end > ints.Length)
            {
                throw new ArgumentException();
            }
            for (int i = start; i < end; i++)
            {
                sum += ints[i];
            }
            res = sum;
        }
    }



}

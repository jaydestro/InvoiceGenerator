using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Invoice

{
    public class DBStorage
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobContainerName { get; set; }

        IMongoDatabase database;
        public IMongoCollection<Invoice> Collection { get; set; }
        BlobContainerClient blobContainer;

        public DBStorage(string connectionString, string databaseName, string collectionName, string blobConnectionString, string blobContainerName)
        {
            this.ConnectionString = connectionString;
            this.DatabaseName = databaseName;
            this.CollectionName = collectionName;
            this.BlobConnectionString = blobConnectionString;
            this.BlobContainerName = blobContainerName;
        }

        public void CreateDatabaseAndStorage()
        {
            MongoClient client = new MongoClient(this.ConnectionString);
            this.database = client.GetDatabase(this.DatabaseName);
            this.Collection = database.GetCollection<Invoice>(this.CollectionName);

            // Create the database and container if they don't exist
            CreateDatabase();

            blobContainer = new BlobContainerClient(this.BlobConnectionString, this.BlobContainerName);
            CreateBlobContainer(this.BlobContainerName);
        }
        private void CreateDatabase()
        {
            var databaseList = this.database.Client.ListDatabaseNames().ToList();

            if (!databaseList.Contains(this.DatabaseName))
            {
                this.database.Client.GetDatabase(this.DatabaseName);
            }
        }
        private void CreateBlobContainer(string containerName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(this.BlobConnectionString);
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(this.BlobContainerName);

            bool containerExists = blobContainerClient.Exists();

            if (!containerExists)
            {
                blobContainerClient.Create();
                blobContainerClient.SetAccessPolicy(PublicAccessType.Blob);
                Console.WriteLine("Blob storage container created successfully and anonymous access enabled.");
            }
            else
            {
                Console.WriteLine("Blob storage container already exists.");
            }
        }
        public bool IsDatabaseAvailable()
        {
            try
            {
                this.database.RunCommand((Command<BsonDocument>)"{ping:1}");
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void UploadPdfToBlobStorage(string fileName, byte[] fileBytes)
        {
            // Upload the PDF file to Azure Blob Storage
            using (MemoryStream stream = new MemoryStream(fileBytes))
            {
                this.blobContainer.UploadBlob(fileName, stream);
            }
        }

        public void DeletePdfFromBlobStorage(string fileName) 
        {
            this.blobContainer.DeleteBlob(fileName);
        }

        public string GetBlobStorageUrl(string fileName)
        {
            // Get the public URL of the uploaded PDF file
            BlobClient blobClient = this.blobContainer.GetBlobClient(fileName);
            return blobClient.Uri.AbsoluteUri;
        }

        public List<Invoice> ListAll()
        {
            return this.Collection.Find(new BsonDocument()).ToList();

        }
    }
}

using MongoDB.Driver;
using MongoDB.Bson;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;

namespace Invoice
{
    public class DBStorage
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobContainerName { get; set; }

        private IMongoCollection<Invoice>? _collection;
        public IMongoCollection<Invoice>? Collection
        {
            get => _collection;
            set => _collection = value;
        }

        private IMongoDatabase? database;

        private readonly Lazy<BlobContainerClient> lazyBlobContainer;

        public int IncrementalInvoiceNumber { get; set; }

        public DBStorage(string connectionString, string databaseName, string collectionName, string blobConnectionString, string blobContainerName)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            BlobConnectionString = blobConnectionString ?? throw new ArgumentNullException(nameof(blobConnectionString));
            BlobContainerName = blobContainerName ?? throw new ArgumentNullException(nameof(blobContainerName));

            lazyBlobContainer = new Lazy<BlobContainerClient>(() => new BlobContainerClient(BlobConnectionString, BlobContainerName));

            IncrementalInvoiceNumber = GetHighestInvoiceNumber();
        }

        private BlobContainerClient BlobContainer => lazyBlobContainer.Value;

        public IMongoCollection<Invoice> GetDatabaseCollection()
        {
            return Collection ?? throw new InvalidOperationException("Mongo Collection is null.");
        }

        public void CreateDatabaseAndStorage()
        {
            MongoClient client = new MongoClient(ConnectionString);
            database = client.GetDatabase(DatabaseName);
            Collection = database.GetCollection<Invoice>(CollectionName);

            CreateDatabase();
            CreateBlobContainer(BlobContainerName);
        }

        private void CreateDatabase()
        {
            if (database == null)
            {
                throw new InvalidOperationException("Database is null");
            }

            var databaseList = database.Client.ListDatabaseNames().ToList();
            if (!databaseList.Contains(DatabaseName))
            {
                database.Client.GetDatabase(DatabaseName);
            }
        }

        private void CreateBlobContainer(string containerName)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(BlobConnectionString);
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(BlobContainerName);

            bool containerExists = blobContainerClient.Exists();
            if (!containerExists)
            {
                blobContainerClient.Create();
                blobContainerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
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
                database?.RunCommand((Command<BsonDocument>)"{ping:1}");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UploadPdfToBlobStorage(string fileName, byte[] fileBytes)
        {
            using (MemoryStream stream = new MemoryStream(fileBytes))
            {
                BlobContainer.UploadBlob(fileName, stream);
            }
        }

        public void DeletePdfFromBlobStorage(string fileName)
        {
            try
            {
                BlobContainer.DeleteBlob(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting blob: {ex.Message}");
            }
        }

        public string GetBlobStorageUrl(string fileName)
        {
            BlobClient blobClient = BlobContainer.GetBlobClient(fileName);
            return blobClient.Uri?.AbsoluteUri ?? string.Empty;
        }

        public List<Invoice> ListAll()
        {
            return Collection?.Find(new BsonDocument()).ToList() ?? new List<Invoice>();
        }

        public int GetInvoiceCount()
        {
            return ListAll().Count;
        }

        public List<Invoice> ListActive()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", false);
            return Collection?.Find(filter).ToList() ?? new List<Invoice>();
        }

        public List<Invoice> ListDeleted()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", true);
            return Collection?.Find(filter).ToList() ?? new List<Invoice>();
        }

        public void Delete(Invoice invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var filter = Builders<Invoice>.Filter.Eq("_id", invoice.Id);
            var update = Builders<Invoice>.Update.Set("IsDeleted", true);
            Collection?.UpdateOne(filter, update);
        }

        public void UnDelete(Invoice invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var filter = Builders<Invoice>.Filter.Eq("_id", invoice.Id);
            var update = Builders<Invoice>.Update.Set("IsDeleted", false);
            Collection?.UpdateOne(filter, update);
        }

        private int GetHighestInvoiceNumber()
        {
            var filter = Builders<Invoice>.Filter.Empty;
            var sort = Builders<Invoice>.Sort.Descending("IncrementalInvoiceNumber");
            var result = Collection?.Find(filter).Sort(sort).Limit(1).FirstOrDefault();
            return result?.IncrementalInvoiceNumber ?? 0;
        }
    }
}
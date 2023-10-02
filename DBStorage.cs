using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace Invoice
{
    public class DBStorage
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobContainerName { get; set; }

public IMongoCollection<Invoice>? Collection { get; set; }
private IMongoDatabase? database;

        private Lazy<BlobContainerClient> lazyBlobContainer;

        public DBStorage(string connectionString, string databaseName, string collectionName, string blobConnectionString, string blobContainerName)
        {
            this.ConnectionString = connectionString;
            this.DatabaseName = databaseName;
            this.CollectionName = collectionName;
            this.BlobConnectionString = blobConnectionString;
            this.BlobContainerName = blobContainerName;

            this.lazyBlobContainer = new Lazy<BlobContainerClient>(() => new BlobContainerClient(BlobConnectionString, BlobContainerName));
        }

        private BlobContainerClient BlobContainer => lazyBlobContainer.Value;

        public void CreateDatabaseAndStorage()
        {
            MongoClient client = new MongoClient(this.ConnectionString);
            this.database = client.GetDatabase(this.DatabaseName);
            this.Collection = database.GetCollection<Invoice>(this.CollectionName);

            // Create the database and container if they don't exist
            CreateDatabase();

            CreateBlobContainer(this.BlobContainerName);
        }

        private void CreateDatabase()
        {
            if (database is null)
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
                if (database is null)
                {
                    throw new InvalidOperationException("Database is null");
                }

                database.RunCommand((Command<BsonDocument>)"{ping:1}");
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
                this.BlobContainer.UploadBlob(fileName, stream);
            }
        }

        public void DeletePdfFromBlobStorage(string fileName) 
        {
            this.BlobContainer.DeleteBlob(fileName);
        }

        public string GetBlobStorageUrl(string fileName)
        {
            BlobClient blobClient = this.BlobContainer.GetBlobClient(fileName);
            return blobClient.Uri.AbsoluteUri;
        }

        public List<Invoice> ListAll()
        {
            return this.Collection.Find(new BsonDocument()).ToList();
        }

        public int GetInvoiceCount() 
        {
            return this.ListAll().Count;
        }
        
        public List<Invoice> ListActive()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", false);
            return this.Collection.Find(filter).ToList();
        }

        public List<Invoice> ListDeleted()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", true);
            return this.Collection.Find(filter).ToList();
        }
    }
}

using MongoDB.Driver;
using MongoDB.Bson;
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

        private IMongoCollection<Invoice>? _collection;
        public IMongoCollection<Invoice>? Collection
        {
            get => _collection;
            set => _collection = value;
        }

        private IMongoDatabase? database;
        private readonly Lazy<BlobContainerClient> lazyBlobContainer;

        private int? _incrementalInvoiceNumber = null;
        public int IncrementalInvoiceNumber
        {
            get
            {
                if (!_incrementalInvoiceNumber.HasValue)
                {
                    _incrementalInvoiceNumber = GetHighestInvoiceNumber();
                }
                return _incrementalInvoiceNumber.Value;
            }
        }

        public int InvoiceNumber { get; private set; }

        public DBStorage(string connectionString, string databaseName, string collectionName, string blobConnectionString, string blobContainerName)
        {
            this.ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            this.DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            this.CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            this.BlobConnectionString = blobConnectionString ?? throw new ArgumentNullException(nameof(blobConnectionString));
            this.BlobContainerName = blobContainerName ?? throw new ArgumentNullException(nameof(blobContainerName));

            this.lazyBlobContainer = new Lazy<BlobContainerClient>(() => new BlobContainerClient(BlobConnectionString, BlobContainerName));
        }

        private BlobContainerClient BlobContainer => lazyBlobContainer.Value;

        public IMongoCollection<Invoice> GetDatabaseCollection()
        {
            return this.Collection ?? throw new InvalidOperationException("Mongo Collection is null.");
        }

        public void CreateDatabaseAndStorage()
        {
            MongoClient client = new MongoClient(this.ConnectionString);
            this.database = client.GetDatabase(this.DatabaseName);
            this.Collection = database.GetCollection<Invoice>(this.CollectionName);

            // Create the database if it doesn't exist
            CreateDatabase();

            // Create the collection if it doesn't exist
            CreateCollection();

            // Create an index on the 'InvoiceNumber' field
            CreateInvoiceNumberIndex();

            // Create the Azure Blob Storage container
            CreateBlobContainer(this.BlobContainerName);
        }

        private void CreateCollection()
        {
            if (database is null)
            {
                throw new InvalidOperationException("Database is null");
            }

            var collectionNames = database.ListCollectionNames().ToList();

            if (!collectionNames.Contains(CollectionName))
            {
                database.CreateCollection(CollectionName);
                Console.WriteLine($"Collection '{CollectionName}' created successfully.");
            }
            else
            {
                Console.WriteLine($"Collection '{CollectionName}' already exists.");
            }
        }

        private void CreateInvoiceNumberIndex()
        {
            if (Collection is null)
            {
                throw new InvalidOperationException("Mongo Collection is null.");
            }

            var indexKeysDefinition = Builders<Invoice>.IndexKeys.Ascending(x => x.InvoiceNumber);
            var indexModel = new CreateIndexModel<Invoice>(indexKeysDefinition);
            Collection.Indexes.CreateOne(indexModel);
        }

        private void CreateDatabase()
        {
            if (database is null)
            {
                throw new InvalidOperationException("Database is null");
            }

            var databaseList = database.Client.ListDatabaseNames()?.ToList();

            if (databaseList != null && !databaseList.Contains(DatabaseName))
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
                BlobClient blobClient = this.BlobContainer.GetBlobClient(fileName);

                // Check if blob already exists
                if (blobClient.Exists())
                {
                    // Handle blob already existing, e.g., rename the fileName or decide to overwrite.
                    // Below is a simple renaming strategy by appending a new GUID. Adjust as needed.
                    fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.pdf";
                    blobClient = this.BlobContainer.GetBlobClient(fileName);
                }

                blobClient.Upload(stream, overwrite: true); // Now it should be safe to use overwrite: true
            }
        }

        public void DeletePdfFromBlobStorage(string fileName)
        {
            try
            {
                this.BlobContainer.DeleteBlob(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting blob: {ex.Message}");
            }
        }

        public string GetBlobStorageUrl(string fileName)
        {
            BlobClient blobClient = this.BlobContainer.GetBlobClient(fileName);
            return blobClient.Uri?.AbsoluteUri ?? string.Empty;
        }

        public List<Invoice> ListAll()
        {
            return this.Collection?.Find(new BsonDocument())?.ToList() ?? new List<Invoice>();
        }

        public int GetInvoiceCount()
        {
            return this.ListAll()?.Count ?? 0;
        }

        public List<Invoice> ListActive()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", false);
            return this.Collection?.Find(filter)?.ToList() ?? new List<Invoice>();
        }

        public List<Invoice> ListDeleted()
        {
            var filter = Builders<Invoice>.Filter.Eq("IsDeleted", true);
            return this.Collection?.Find(filter)?.ToList() ?? new List<Invoice>();
        }

        public void Delete(Invoice invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var filter = Builders<Invoice>.Filter.Eq("_id", invoice.Id);
            var update = Builders<Invoice>.Update.Set("IsDeleted", true);
            this.Collection?.UpdateOne(filter, update);

            // After successfully deleting the invoice, delete the corresponding PDF
            DeletePdfFromBlobStorage(GetPdfFileName(invoice));
        }

        public void UnDelete(Invoice invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var filter = Builders<Invoice>.Filter.Eq("_id", invoice.Id);
            var update = Builders<Invoice>.Update.Set("IsDeleted", false);
            this.Collection?.UpdateOne(filter, update);

            // After successfully undeleting the invoice, generate and store a new PDF
            GenerateAndStorePdfForInvoice(invoice);
        }

        // Helper method to generate and store a new PDF for an invoice
        private void GenerateAndStorePdfForInvoice(Invoice invoice)
        {
            var pdfFileName = GetPdfFileName(invoice);
            var pdfBytes = invoice.GeneratePdf();
            UploadPdfToBlobStorage(pdfFileName, pdfBytes);
        }

        // Helper method to get the PDF file name based on the invoice number
        private string GetPdfFileName(Invoice invoice)
        {
            return $"{invoice.InvoiceNumber:00000}_invoice.pdf";
        }

        public int GetHighestInvoiceNumber()
        {
            var projection = Builders<Invoice>.Projection.Include(x => x.InvoiceNumber);
            var sort = Builders<Invoice>.Sort.Descending(x => x.InvoiceNumber);
            var highestInvoice = Collection.Find(Builders<Invoice>.Filter.Empty)
                                           .Project<Invoice>(projection) // Use Project<Invoice> to project to the Invoice class
                                           .Sort(sort)
                                           .FirstOrDefault();

            if (highestInvoice != null)
            {
                return highestInvoice.InvoiceNumber;
            }
            else
            {
                return 0; // Return 0 if no invoices exist in the database.
            }
        }
    }
}
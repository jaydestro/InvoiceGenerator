// TBD: make sure invoices numbers arent replicated in the database, and the invoice pdfs arent replicated in the blob storage
// deserialize the invoice pdfs from the blob storage and store them in the database

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Invoice
{

    public class Invoice
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("IncrementalInvoiceNumber")]
        public int IncrementalInvoiceNumber { get; set; }

        [BsonElement("InvoiceNumber")]
        public int InvoiceNumber { get; set; }

        [BsonElement("Date")]
        public DateTime Date { get; set; }

        [BsonElement("FullName")]
        public string FullName { get; set; }

        [BsonElement("Items")]
        public List<Item> Items { get; set; }

        [BsonElement("Tax")]
        public decimal Tax { get; set; }

        [BsonElement("TotalCost")]
        public decimal TotalCost { get; set; }

        [BsonElement("PdfUrl")]
        public string PdfUrl { get; set; }

        [BsonElement("IsDeleted")]
        public bool IsDeleted { get; set; }

        [BsonIgnore]
        private static int lastUsedInvoiceNumber = 0;

        [BsonIgnore]
        private static object invoiceNumberLock = new object();

        public Invoice() {
            Items = new List<Item>();
            Tax = 0M;
            TotalCost = 0M;
            PdfUrl = string.Empty;
            IsDeleted = false;
        }
        public string AddInvoice(DBStorage databaseAndStorage)
        {
            // Save the invoice to the MongoDB collection
            databaseAndStorage.Collection.InsertOne(this);
            Console.WriteLine("Invoice saved to the MongoDB database.");
            // Generate and store the PDF invoice
            string pdfFileName = String.Format("{0:00000}_invoice.pdf", this.InvoiceNumber);
            byte[] pdfBytes = GeneratePdfInvoice();
            databaseAndStorage.UploadPdfToBlobStorage(pdfFileName, pdfBytes);

            // Get the public URL of the PDF invoice
            string pdfUrl = databaseAndStorage.GetBlobStorageUrl(pdfFileName);
            Console.WriteLine("PDF invoice stored in Azure Blob Storage.");

            // Store the PDF URL in the MongoDB invoice document
            var filter = Builders<Invoice>.Filter.Eq("_id", this.Id);
            var update = Builders<Invoice>.Update.Set("PdfUrl", pdfUrl);
            databaseAndStorage.Collection.UpdateOne(filter, update);

            return pdfUrl;
        }

        public bool DeleteInvoice(DBStorage databaseAndStorage) {
            try {
                databaseAndStorage.DeletePdfFromBlobStorage(this.PdfUrl.Split('/').Last());
                var filter = Builders<Invoice>.Filter.Eq("_id", this.Id);
                var update = Builders<Invoice>.Update.Set("IsDeleted", true);
                databaseAndStorage.Collection.UpdateOne(filter, update);
                return true;
            }
            catch (Exception e) {
                Console.WriteLine("Error: {0}", e.Message);
                return false;
            }
        }

        public byte[] GeneratePdfInvoice()
        {
            // Create a new PDF document
            Document document = new Document();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Create a writer to write the PDF contents to the memory stream
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                // Add the invoice details to the PDF document
                document.Add(new Paragraph("INVOICE DETAILS"));
                document.Add(Chunk.NEWLINE);
                document.Add(new Paragraph($"Invoice Number: {this.InvoiceNumber :00000}"));
                document.Add(new Paragraph($"Date of Invoice: {this.Date:yyyy-MM-dd}"));
                document.Add(new Paragraph($"Customer Name: {this.FullName}"));
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph("Items:"));
                foreach (Item item in this.Items)
                {
                    document.Add(new Paragraph($"Item: {item.Name}, Price: {item.Price:C}, Quantity: {item.Quantity}, Shipping Cost: {item.ShippingCost:C}"));
                }
                document.Add(Chunk.NEWLINE);

                document.Add(new Paragraph($"Sales Tax: {this.Tax:C}"));
                document.Add(new Paragraph($"Total Cost: {this.TotalCost:C}"));

                document.Close();

                // Return the PDF contents as a byte array
                return memoryStream.ToArray();
            }
        }

    }
}

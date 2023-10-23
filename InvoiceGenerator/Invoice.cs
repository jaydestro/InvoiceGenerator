using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Azure;

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
        public string? FullName { get; set; }

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

        public Invoice()
        {
            Items = new List<Item>();
            Tax = 0M;
            TotalCost = 0M;
            PdfUrl = string.Empty;
            IsDeleted = false;
            FullName = null;
        }

        public string AddInvoice(IDBStorage databaseAndStorage)
        {
            // Fetch the latest incremental invoice number from the database
            int currentIncrementalNumber = databaseAndStorage.GetHighestInvoiceNumber();

            // Generate a unique invoice number
            this.InvoiceNumber = currentIncrementalNumber + 1;

            // Save the invoice to the MongoDB collection
            var collection = databaseAndStorage.GetDatabaseCollection();
            collection.InsertOne(this);
            Console.WriteLine("Invoice saved to the MongoDB database.");

            // Generate and store the PDF invoice
            var pdfUrl = this.GeneratePdfInvoice(databaseAndStorage);

            return pdfUrl;
        }

        public string GeneratePdfInvoice(IDBStorage databaseAndStorage)
        {
            string pdfFileName = String.Format("{0:00000}_invoice.pdf", this.InvoiceNumber);
            byte[] pdfBytes = GeneratePdf();
            databaseAndStorage.UploadPdfToBlobStorage(pdfFileName, pdfBytes);

            // Get the public URL of the PDF invoice
            string pdfUrl = databaseAndStorage.GetBlobStorageUrl(pdfFileName);
            Console.WriteLine("PDF invoice stored in Azure Blob Storage.");

            // Store the PDF URL in the MongoDB invoice document
            var filter = Builders<Invoice>.Filter.Eq("_id", this.Id);
            var update = Builders<Invoice>.Update.Set("PdfUrl", pdfUrl);
            var collection = databaseAndStorage.GetDatabaseCollection();
            collection.UpdateOne(filter, update);

            return pdfUrl;
        }

        public bool DeleteInvoice(IDBStorage databaseAndStorage)
        {
            try
            {
                // Delete the PDF from Blob Storage
                string pdfFileName = this.PdfUrl.Split('/').Last();
                databaseAndStorage.DeletePdfFromBlobStorage(pdfFileName);

                // Mark the invoice as deleted in the database
                var filter = Builders<Invoice>.Filter.Eq("_id", this.Id);
                var update = Builders<Invoice>.Update.Set("IsDeleted", true);
                var collection = databaseAndStorage.GetDatabaseCollection();
                collection.UpdateOne(filter, update);

                Console.WriteLine($"Invoice {this.InvoiceNumber:00000} and PDF deleted successfully.");
                return true;
            }
            catch (Azure.RequestFailedException ex)
            {
                Console.WriteLine($"Error deleting PDF from Blob Storage: {ex.Message}");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return false;
            }
        }

        public string UndeleteInvoice(IDBStorage databaseAndStorage)
        {
            var filter = Builders<Invoice>.Filter.Eq("_id", this.Id);
            var update = Builders<Invoice>.Update.Set("IsDeleted", false);
            var collection = databaseAndStorage.GetDatabaseCollection();
            collection.UpdateOne(filter, update);

            var pdfUrl = this.GeneratePdfInvoice(databaseAndStorage);
            return pdfUrl;
        }

        public byte[] GeneratePdf()
        {
            using (MemoryStream memoryStream = new())
            {
                PdfWriter writer = new(memoryStream);
                PdfDocument pdfDoc = new(writer);
                Document document = new(pdfDoc);

                document.Add(new Paragraph("INVOICE DETAILS"));
                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph($"Invoice Number: {this.InvoiceNumber:00000}"));
                document.Add(new Paragraph($"Date of Invoice: {this.Date:yyyy-MM-dd}"));
                document.Add(new Paragraph($"Customer Name: {this.FullName}"));
                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph("Items:"));
                foreach (Item item in this.Items)
                {
                    document.Add(new Paragraph($"Item: {item.Name}, Price: {item.Price:C}, Quantity: {item.Quantity}, Shipping Cost: {item.ShippingCost:C}"));
                }
                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph($"Sales Tax: {this.Tax:C}"));
                document.Add(new Paragraph($"Total Cost: {this.TotalCost:C}"));

                document.Close();

                return memoryStream.ToArray();
            }
        }
    }
}
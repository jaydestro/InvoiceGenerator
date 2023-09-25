using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Invoice;

class InvoiceGenerator
{
    static IConfiguration configuration;

    public static void Main()
    {
        configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = configuration.GetConnectionString("MongoDB");
        string databaseName = "invoices";
        string collectionName = "invoices";
        string blobStorageConnectionString = configuration.GetConnectionString("BlobStorage");
        string containerName = configuration["BlobStorageSettings:ContainerName"];

        var databaseAndStorage = new DBStorage(connectionString, databaseName, collectionName, blobStorageConnectionString, containerName);
        databaseAndStorage.CreateDatabaseAndStorage();


        while (true)
        {
            Console.WriteLine("What would you like to do?");
            Console.WriteLine("1. Add a new invoice");
            Console.WriteLine("2. List existing invoices");
            Console.WriteLine("3. Delete an invoice");
            Console.WriteLine("4. Exit");
            Console.Write("Enter your choice (1, 2, 3, or 4): ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    AddNewInvoice(databaseAndStorage);
                    break;
                case "2":
                    ListAndShowExistingInvoices(databaseAndStorage);
                    break;
                case "3":
                    DeleteInvoice(databaseAndStorage);
                    break;
                case "4":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }
    private static void AddNewInvoice(DBStorage databaseAndStorage)
    {
        // List out current invoices to set the correct receipt number
        var existingInvoices = databaseAndStorage.GetInvoiceCount();

        Console.WriteLine("What is your first name?");
        string firstName = Console.ReadLine();

        Console.WriteLine("What is your last name?");
        string lastName = Console.ReadLine();

        var itemList = new List<Item>();

        bool keepGoing = true;
        int itemCount = 1;
        while (keepGoing)
        {
            Console.Write($"Enter the name of item #{itemCount} (or leave it empty to finish): ");
            string itemName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                keepGoing = false;
            }
            else
            {
                var item = new Item(itemName);
                Console.Write($"Enter the quantity of item #{itemCount}: ");
                int quantity = 0;
                bool success = false;
                while (!success || quantity == 0) {
                    success = int.TryParse(Console.ReadLine(), out quantity);
                    if (success && quantity > 0)
                    {
                        item.Quantity = quantity;
                    }
                    else
                    {
                        Console.WriteLine("Invalid quantity. Please try again.");
                        Console.WriteLine("Enter the quantity of item #{0}: ", itemCount);
                    }
                } 

                Console.Write($"Enter the price of item #{itemCount}: ");
                decimal price = 0M;
                success = false;
                while (!success || price == 0M) {
                    success = decimal.TryParse(Console.ReadLine(), out price);
                    if (success && price > 0M)
                    {
                        item.Price = price;
                    }
                    else
                    {
                        Console.WriteLine("Invalid price. Please try again.");
                        Console.WriteLine("Enter the price of item #{0}: ", itemCount);
                    }
                } 

                Console.Write($"Enter the shipping cost for the entire quantity of item #{itemCount}: ");
                decimal shippingCost = 0M;
                success = false;
                while (!success || shippingCost == 0M) {
                    success = decimal.TryParse(Console.ReadLine(), out shippingCost);
                    if (success && shippingCost > 0M)
                    {
                        item.ShippingCost = shippingCost;
                    }
                    else
                    {
                        Console.WriteLine("Invalid shipping costs. Please try again.");
                        Console.WriteLine("Enter the shipping costs of item #{0}: ", itemCount);
                    }
                } 

                itemList.Add(item);
                itemCount++;
            }
        }

        decimal subTotal = 0M;
        foreach (Item item in itemList)
        {
            subTotal += (item.Price * item.Quantity);
        }

        string stateCode;
        decimal salesTaxRate;

        do
        {
            Console.Write("Enter the two-letter US state code (e.g., NY) or type 'list' to see the sales tax rates: ");
            stateCode = Console.ReadLine().ToUpper();

            if (stateCode == "LIST")
            {
                ListAllStatesTaxRates();
            }
        } while (stateCode == "LIST");

        salesTaxRate = GetSalesTaxRate(stateCode);

        while (salesTaxRate == 0M)
        {
            Console.WriteLine("Invalid state code. Unable to calculate sales tax.");
            stateCode = Console.ReadLine().ToUpper();
            salesTaxRate = GetSalesTaxRate(stateCode);
        }

        decimal totalTax = Math.Round((subTotal * salesTaxRate / 100), 2);
        var fullName = $"{firstName} {lastName}";
        var totalCost = subTotal + totalTax;

        var todayDate = DateTime.Now;
        var invoice = new Invoice.Invoice
        {
            InvoiceNumber = existingInvoices++,
            Date = todayDate,
            FullName = fullName,
            Items = itemList,
            Tax = totalTax,
            TotalCost = totalCost
        };

        if (databaseAndStorage.IsDatabaseAvailable())
        {
            var pdfUrl = invoice.AddInvoice(databaseAndStorage);

            // Print invoice details to the console
            Console.WriteLine("\n__INVOICE DETAILS__");
            Console.WriteLine("Invoice Number: {0:00000}", invoice.InvoiceNumber);
            Console.WriteLine("Date of Invoice: {0:yyyy-MM-dd}", invoice.Date);
            Console.WriteLine("Customer Name: {0}", invoice.FullName);
            foreach (Item item in invoice.Items)
            {
                Console.WriteLine("Item: {0}, Price: {1:C}, Quantity: {2}, Shipping Cost: {3:C}", item.Name, item.Price, item.Quantity, item.ShippingCost);
            }
            Console.WriteLine("Sales Tax: {0:C}", invoice.Tax);
            Console.WriteLine("Total Cost: {0:C}", invoice.TotalCost);
            Console.WriteLine("PDF Invoice URL: {0}", pdfUrl);
        }
        else
        {
            Console.WriteLine("Unable to connect to the MongoDB database. Please try again later.");
        }
    }

    private static List<Invoice.Invoice> ListExistingInvoices(DBStorage databaseAndStorage) {

        var invoices = databaseAndStorage.ListActive();

        if (invoices.Count > 0)
        {
            Console.WriteLine("Existing Invoices:");

            for (int i = 0; i < invoices.Count; i++)
            {
                var invoice = invoices[i];
                Console.WriteLine("{0}. {1:00000} - {2} - {3:yyyy-MM-dd} - Total: {4:C}", i + 1, invoice.InvoiceNumber, invoice.FullName, invoice.Date, invoice.TotalCost);
            }
        }
        return invoices;

    }

    private static void ListAndShowExistingInvoices(DBStorage databaseAndStorage)

    {
        var invoices = ListExistingInvoices(databaseAndStorage);

        if (invoices.Count > 0)
        {
            
            Console.Write("Select an invoice by number (1-{0}) or enter 0 to cancel: ", invoices.Count);
            int invoiceIndex;
            if (int.TryParse(Console.ReadLine(), out invoiceIndex))
            {
                if (invoiceIndex >= 1 && invoiceIndex <= invoices.Count)
                {
                    var invoice = invoices[invoiceIndex - 1];

                    Console.WriteLine("\nSelected Invoice:");
                    Console.WriteLine("Invoice Number: {0:00000}", invoice.InvoiceNumber);
                    Console.WriteLine("Date of Invoice: {0:yyyy-MM-dd}", invoice.Date);
                    Console.WriteLine("Customer Name: {0}", invoice.FullName);
                    foreach (Item item in invoice.Items)
                    {
                        Console.WriteLine("Item: {0}, Price: {1:C}, Quantity: {2}, Shipping Cost: {3:C}", item.Name, item.Price, item.Quantity, item.ShippingCost);
                    }
                    Console.WriteLine("Sales Tax: {0:C}", invoice.Tax);
                    Console.WriteLine("Total Cost: {0:C}", invoice.TotalCost);
                    Console.WriteLine("PDF Invoice URL: {0}", invoice.PdfUrl);
                }
                else if (invoiceIndex == 0)
                {
                    Console.WriteLine("Invoice selection canceled.");
                }
                else
                {
                    Console.WriteLine("Invalid invoice number. Please try again.");
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Please try again.");
            }
        }
        else
        {
            Console.WriteLine("No existing invoices found.");
        }
    }

    private static void ListAllStatesTaxRates()
    {
        var stateSalesTaxRates = LoadStateSalesTaxRates();

        Console.WriteLine("\nSales Tax Rates by State:");
        foreach (var stateTax in stateSalesTaxRates)
        {
            Console.WriteLine("{0}: {1}%", stateTax.Key, stateTax.Value);
        }
    }

  private static void DeleteInvoice(DBStorage databaseAndStorage)
{
    var invoices = ListExistingInvoices(databaseAndStorage); // Display the existing invoices to choose from

    Console.Write("Select an invoice by number to delete (1-{0}) or enter 0 to cancel: ", invoices.Count);
    if (int.TryParse(Console.ReadLine(), out int invoiceIndex))
    {
        if (invoiceIndex >= 1 && invoiceIndex <= invoices.Count)
        {
            var invoiceToDelete = invoices[invoiceIndex - 1];
            if (invoiceToDelete.DeleteInvoice(databaseAndStorage))
            {
                Console.WriteLine("Invoice deleted successfully.");
            }
            else
            {
                Console.WriteLine("Failed to delete the invoice.");
            }
        }
        else if (invoiceIndex == 0)
        {
            Console.WriteLine("Invoice deletion canceled.");
        }
        else
        {
            Console.WriteLine("Invalid invoice number. Please try again.");
        }
    }
    else
    {
        Console.WriteLine("Invalid input. Please try again.");
    }
}
  

    private static decimal GetSalesTaxRate(string stateCode)
    {
        var stateSalesTaxRates = LoadStateSalesTaxRates();
        if (stateSalesTaxRates.ContainsKey(stateCode))
        {
            return stateSalesTaxRates[stateCode];
        }
        return 0M;
    }

    private static Dictionary<string, decimal> LoadStateSalesTaxRates()
    {
        var json = File.ReadAllText("stateSalesTaxRates.json");
        var stateSalesTaxRates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json);
        return stateSalesTaxRates;
    }

}

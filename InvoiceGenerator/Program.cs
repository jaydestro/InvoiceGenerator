using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using Invoice;
using Sharprompt;

class InvoiceGenerator
{
    static IConfiguration? configuration;

    public static void Main()
    {
        configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = configuration.GetConnectionString("MongoDB") ?? throw new ArgumentNullException(nameof(connectionString));
        string databaseName = "invoices";
        string collectionName = "invoices";
        string blobStorageConnectionString = configuration.GetConnectionString("BlobStorage") ?? throw new ArgumentNullException(nameof(blobStorageConnectionString));
        string containerName = configuration["BlobStorageSettings:ContainerName"] ?? throw new ArgumentNullException(nameof(containerName));

        var databaseAndStorage = new DBStorage(connectionString, databaseName, collectionName, blobStorageConnectionString, containerName);
        databaseAndStorage.CreateDatabaseAndStorage();

        while (true)
        {
            var options = new[] { "Add a new invoice", "List existing invoices", "Delete an invoice", "Undelete an invoice", "Exit" };
            var choice = Prompt.Select("What would you like to do?", options);

            switch (choice)
            {
                case "Add a new invoice":
                    AddNewInvoice(databaseAndStorage);
                    break;
                case "List existing invoices":
                    ListAndShowExistingInvoices(databaseAndStorage);
                    break;
                case "Delete an invoice":
                    DeleteInvoice(databaseAndStorage);
                    break;
                case "Undelete an invoice":
                    UnDeleteInvoice(databaseAndStorage);
                    break;
                case "Exit":
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
        string firstName;
        do
        {
            firstName = Prompt.Input<string>("What is your first name?");
        } while (string.IsNullOrWhiteSpace(firstName));

        string lastName;
        do
        {
            lastName = Prompt.Input<string>("What is your last name?");
        } while (string.IsNullOrWhiteSpace(lastName));


        var itemList = new List<Item>();

        while (true)
        {
            var itemName = Prompt.Input<string>($"Enter the name of the item (or leave it empty to finish)");

            // Check if the entered item name is empty and the list is empty
            if (string.IsNullOrWhiteSpace(itemName) && itemList.Count == 0)
            {
                Console.WriteLine("You must enter at least one item.");
                continue; // Continue the loop to prompt for the first item
            }
            else if (string.IsNullOrWhiteSpace(itemName))
            {
                break; // Exit the loop when an empty item name is entered after the first item
            }

            var item = new Item(itemName);
            item.Quantity = Prompt.Input<int>($"Enter the quantity of {itemName}", validators: new[] { QuantityValidator() });
            item.Price = Prompt.Input<decimal>($"Enter the price of {itemName}", validators: new[] { PriceValidator() });

            // Prompt for shipping cost without making it optional
            decimal? shippingCost = null;
            while (true)
            {
                var shippingCostInput = Prompt.Input<string>($"Enter the shipping cost for the entire quantity of {itemName} (or leave it empty to skip)");
                if (string.IsNullOrWhiteSpace(shippingCostInput))
                {
                    break; // Skip shipping cost input
                }

                if (decimal.TryParse(shippingCostInput, out var parsedShippingCost))
                {
                    shippingCost = parsedShippingCost;
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please enter a valid shipping cost.");
                }
            }

            item.ShippingCost = shippingCost ?? 0;
            itemList.Add(item);
        }

        decimal subTotal = itemList.Sum(item => item.Price * item.Quantity);

        decimal salesTaxRate;
        do
        {
            var stateCode = Prompt.Input<string>("Enter the two-letter US state code (e.g., NY) or type 'LIST' to see the sales tax rates");
            if (stateCode.Equals("LIST", StringComparison.OrdinalIgnoreCase))
            {
                ListAllStatesTaxRates();
                continue; // Restart the loop to ask for the state code again
            }

            salesTaxRate = GetSalesTaxRate(stateCode);

            if (salesTaxRate == 0)
            {
                Console.WriteLine("Invalid state code. Please enter a valid two-letter US state code.");
            }
            else
            {
                break; // Valid state code, exit the loop
            }
        } while (true);

        decimal totalTax = Math.Round(subTotal * salesTaxRate / 100, 2);
        var fullName = $"{firstName} {lastName}";
        var totalCost = subTotal + totalTax;

        var invoice = new Invoice.Invoice
        {
            Date = DateTime.Now,
            FullName = fullName,
            Items = itemList,
            Tax = totalTax,
            TotalCost = totalCost
        };

        if (databaseAndStorage.IsDatabaseAvailable())
        {
            string? pdfUrl = invoice.AddInvoice(databaseAndStorage);

            // Print invoice details to the console
            Console.WriteLine("\n__INVOICE DETAILS__");
            Console.WriteLine($"Invoice Number: {invoice.InvoiceNumber:00000}");
            Console.WriteLine($"Date of Invoice: {invoice.Date:yyyy-MM-dd}");
            Console.WriteLine($"Customer Name: {invoice.FullName}");
            foreach (var item in invoice.Items)
            {
                Console.WriteLine($"Item: {item.Name}, Price: {item.Price:C}, Quantity: {item.Quantity}, Shipping Cost: {item.ShippingCost:C}");
            }
            Console.WriteLine($"Sales Tax: {invoice.Tax:C}");
            Console.WriteLine($"Total Cost: {invoice.TotalCost:C}");

            // Check if pdfUrl is not null before displaying it
            if (pdfUrl != null)
            {
                Console.WriteLine($"PDF Invoice URL: {pdfUrl}");
            }
            else
            {
                Console.WriteLine("PDF Invoice URL is not available.");
            }
        }
        else
        {
            Console.WriteLine("Unable to connect to the MongoDB database. Please try again later.");
        }
    }

    private static Func<object?, ValidationResult?> QuantityValidator()
    {
        return (input) =>
        {
            bool success = false;
            if (input != null)
            {
                if (int.TryParse((string)input, out var quantity))
                {
                    success = true;
                }
            }

            if (success)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Quantity should be greater than 0");
            }
        };
    }

    private static Func<object?, ValidationResult?> PriceValidator()
    {
        return (input) =>
        {
            if (input != null && (decimal)input > 0M)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Prices should be greater than 0");
            }
        };
    }

    private static void ListAndShowExistingInvoices(DBStorage databaseAndStorage)
    {
        var invoices = ListExistingInvoices(databaseAndStorage);

        if (invoices.Count > 0)
        {
            var selectedIndex = Prompt.Input<int>($"Select an invoice by number (1-{invoices.Count}) or enter 0 to cancel", validators: new[] { new Func<object, ValidationResult?>(value => ((int)value) >= 0 && ((int)value) <= invoices.Count ? ValidationResult.Success : new ValidationResult($"Value should be between 0 and {invoices.Count}")) });
            if (selectedIndex > 0)
            {
                var invoice = invoices[selectedIndex - 1];
                Console.WriteLine("\nSelected Invoice:");
                Console.WriteLine($"Invoice Number: {invoice.InvoiceNumber:00000}");
                Console.WriteLine($"Date of Invoice: {invoice.Date:yyyy-MM-dd}");
                Console.WriteLine($"Customer Name: {invoice.FullName}");
                foreach (var item in invoice.Items)
                {
                    Console.WriteLine($"Item: {item.Name}, Price: {item.Price:C}, Quantity: {item.Quantity}, Shipping Cost: {item.ShippingCost:C}");
                }
                Console.WriteLine($"Sales Tax: {invoice.Tax:C}");

                Console.WriteLine($"Total Cost: {invoice.TotalCost:C}");

                // Check if pdfUrl is not null before displaying it
                if (!string.IsNullOrEmpty(invoice.PdfUrl))
                {
                    Console.WriteLine($"PDF Invoice URL: {invoice.PdfUrl}");
                }
                else
                {
                    Console.WriteLine("PDF Invoice URL is not available.");
                }
            }
        }
    }

    private static List<Invoice.Invoice> ListExistingInvoices(DBStorage databaseAndStorage)
    {
        var invoices = databaseAndStorage.ListActive();
        if (invoices.Count > 0)
        {
            Console.WriteLine("Existing Invoices:");
            for (int i = 0; i < invoices.Count; i++)
            {
                var invoice = invoices[i];
                Console.WriteLine($"{i + 1}. {invoice.InvoiceNumber:00000} - {invoice.FullName} - {invoice.Date:yyyy-MM-dd} - Total: {invoice.TotalCost:C}");
            }
        }
        return invoices;
    }

    private static void DeleteInvoice(DBStorage databaseAndStorage)
    {
        var invoices = ListExistingInvoices(databaseAndStorage);

        if (invoices.Count > 0)
        {
            var selectedIndex = Prompt.Input<int>($"Select an invoice by number (1-{invoices.Count}) to delete or enter 0 to cancel", validators: new[] { new Func<object, ValidationResult?>(value => ((int)value) >= 0 && ((int)value) <= invoices.Count ? ValidationResult.Success : new ValidationResult($"Value should be between 0 and {invoices.Count}")) });
            if (selectedIndex > 0)
            {
                var invoice = invoices[selectedIndex - 1];
                databaseAndStorage.Delete(invoice);
                Console.WriteLine($"\nInvoice {invoice.InvoiceNumber:00000} has been deleted.");
            }
        }
    }

    private static void UnDeleteInvoice(DBStorage databaseAndStorage)
    {
        var invoices = databaseAndStorage.ListDeleted();

        if (invoices.Count > 0)
        {
            Console.WriteLine("Deleted Invoices:");
            if (invoices != null)
            {
                for (int i = 0; i < invoices.Count; i++)
                {
                    var invoice = invoices[i];
                    Console.WriteLine($"{i + 1}. {invoice.InvoiceNumber:00000} - {invoice.FullName} - {invoice.Date:yyyy-MM-dd} - Total: {invoice.TotalCost:C}");
                }

                var selectedIndex = Prompt.Input<int>($"Select an invoice by number (1-{invoices.Count}) to undelete or enter 0 to cancel", validators: new[] { new Func<object, ValidationResult?>(value => ((int)value) >= 0 && ((int)value) <= invoices.Count ? ValidationResult.Success : new ValidationResult($"Value should be between 0 and {invoices.Count}")) });
                if (selectedIndex > 0)
                {
                    var invoice = invoices[selectedIndex - 1];
                    databaseAndStorage.UnDelete(invoice);
                    Console.WriteLine($"\nInvoice {invoice.InvoiceNumber:00000} has been undeleted.");
                }
            }
            else
            {
                Console.WriteLine("No deleted invoices found.");
            }
        }
        else
        {
            Console.WriteLine("No deleted invoices found.");
        }
    }

    private static decimal GetSalesTaxRate(string stateCode)
    {
        var salesTaxRates = LoadStateSalesTaxRates();
        return salesTaxRates.ContainsKey(stateCode) ? salesTaxRates[stateCode] : 0m;
    }

    private static void ListAllStatesTaxRates()
    {
        var salesTaxRates = LoadStateSalesTaxRates();
        Console.WriteLine("State sales tax rates:");
        foreach (var kvp in salesTaxRates)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}%");
        }
    }

    private static Dictionary<string, decimal> LoadStateSalesTaxRates()
    {
        var salesTaxRatesJson = File.ReadAllText("StateSalesTaxRates.json");
        return JsonSerializer.Deserialize<Dictionary<string, decimal>>(salesTaxRatesJson) ?? new Dictionary<string, decimal>();
    }
}
# Invoice Generator

The Invoice Generator is a console application that allows you to create and manage invoices. It calculates the sales tax based on the state code provided and stores the invoice data in a MongoDB database. The PDF invoices are stored in Azure Blob Storage with anonymous access, and the public URL of each invoice is also stored in the MongoDB database.

## Features

- Add a new invoice with customer details, items, and shipping costs.
- Calculate sales tax based on the state code and sales tax rates.
- Store invoices in a MongoDB database.
- Generate PDF invoices and store them in Azure Blob Storage.
- Retrieve the public URL of each PDF invoice and store it in the MongoDB database.
- List existing invoices with basic details.
- View detailed information for a selected invoice, including the PDF invoice URL.

## Code Breakdown

The code is structured into several files and classes:

## Program.cs

### Main Method
- Reads configuration settings from `appsettings.json`.
- Creates an instance of `DBStorage` to manage MongoDB and Blob Storage.
- Displays a menu to interact with the application.
- Executes corresponding actions based on user's choices.

### AddNewInvoice Method
- Collects customer information and item details.
- Calculates invoice totals and retrieves sales tax rates.
- Generates an invoice number and creates an `Invoice` instance.
- Stores invoice in MongoDB and generates a PDF version.
- Uploads PDF to Azure Blob Storage and updates PDF URL.
- Displays generated invoice details.

### GeneratePdfInvoice Method
- Uses `iTextSharp` to create a PDF document.
- Adds invoice details to the PDF.
- Returns PDF content as a byte array.

### ListExistingInvoices Method
- Retrieves and lists existing invoices from MongoDB.
- Allows user to select an invoice to display details.

### ListAllStatesTaxRates and GetSalesTaxRate Methods
- Display sales tax rates by state from `stateSalesTaxRates.json`.
- Retrieve sales tax rate for a specific state.

### LoadStateSalesTaxRates Method
- Loads state sales tax rates from `stateSalesTaxRates.json`.

### GenerateRandomInvoiceNumber Method
- Generates a random invoice number.

## Invoice.cs
- Defines the `Invoice` class representing an invoice entity.
- Contains properties for various invoice details.

## Item.cs
- Defines the `Item` class representing an individual item in an invoice.
- Contains properties for item details.

## DBStorage.cs
- Defines the `DBStorage` class for MongoDB and Blob Storage interactions.
- Manages database connections, collections, and blob containers.

## appsettings.json
- Configuration file for application settings, including connection strings.

## stateSalesTaxRates.json
- JSON file containing sales tax rates for different states and territories.

## Getting Started

### Prerequisites

Before running the application, make sure you have the following:

- .NET Core SDK installed
- MongoDB server running on `localhost:27017`
- Azure Blob Storage account with a container for storing the PDF invoices
- Update the `appsettings.json` file with the appropriate connection strings for MongoDB and Azure Blob Storage.

Example `appsettings.json` file:

```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "BlobStorage": "your-blob-storage-connection-string"
  },
  "BlobStorageSettings": {
    "ContainerName": "your-container-name"
  }
}
```

> **Note:** If you have a remote MongoDB server, replace the connection string in the code with the appropriate connection string for your server.
> 
### Get Started with Azure Cosmos DB for MongoDB

You can sign up for Azure Cosmos DB for MongoDB for free and start building scalable applications. To get started, visit [https://aka.ms/trycosmosdb](https://aka.ms/trycosmosdb) to create a free account and explore the powerful features of Azure Cosmos DB.


## How to Use

1. Clone the repository to your local machine.
2. Open a command prompt or terminal and navigate to the project directory.
3. Run the following command to build the application:

   ```shell
    dotnet build
   ```
4. Run the following command to start the application:
   ```shell
    dotnet run
   ```
5. Follow the prompts to interact with the Invoice Generator.
6. Choose option 1 to add a new invoice or option 2 to list existing invoices.

## Technologies Used

- C#
- .NET Core
- MongoDB
- MongoDB .NET Driver
- Azure Blob Storage
- iTextSharp (PDF generation library)

## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvements, please open an issue or submit a pull request.

## License

This project is licensed under the [Apache 2.0 License](LICENSE).

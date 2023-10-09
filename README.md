# Invoice Generator

The Invoice Generator is a console application that allows you to create and manage invoices. It calculates the sales tax based on the state code provided and stores the invoice data in a MongoDB database. The PDF invoices are stored in Azure Blob Storage with anonymous access, and the public URL of each invoice is also stored in the MongoDB database.

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
3. Run the following command to start the application:

   ```shell
    dotnet run
   ```

4. Follow the prompts to interact with the Invoice Generator.
5. Use the menu to create, delete, undelete and show the invoices.

## Invoice Application Code Breakdown

### Overview

This application is a simple invoice generator using MongoDB for storing invoice data and Azure Blob Storage for storing invoice PDFs.

### Structure

The application's structure can be broken down into the following main components:

### Configuration

- `appsettings.json`: Contains connection strings for MongoDB and Azure Blob Storage, and other configuration settings.

### Core Components

1. **DBStorage.cs**: Responsible for database and storage related operations, including:
    - Interactions with MongoDB for CRUD operations.
    - Interactions with Azure Blob Storage for storing invoice PDFs.
    - Initialization of the database and blob storage.

2. **Invoice.cs**: Represents the `Invoice` model and provides functionalities such as:
    - Generating a new invoice PDF.
    - Adding, deleting, and undeleting invoices.

3. **Item.cs**: Represents an item that can be added to an invoice.

4. **Program.cs (InvoiceGenerator)**: This is the main class where the user interacts with the system. It uses the `Sharprompt` library to capture user inputs and present various options.

### Additional Data

- `stateSalesTaxRates.json`: Contains sales tax rates for U.S. states and territories.

## Features

1. **Adding a New Invoice**: Users can input the necessary details to create an invoice, which gets saved to MongoDB and its corresponding PDF stored in Azure Blob Storage.

2. **Listing Invoices**: Users can view a list of existing invoices.

3. **Deleting and Undeleting Invoices**: Users have the option to mark an invoice as deleted or undelete a previously deleted invoice.

4. **Fetching Sales Tax Rates**: Depending on the U.S. state, appropriate sales tax rates are applied to the invoice.

## Enhancements and Notes

- The application currently provides a command-line interface for operations. Consider introducing a graphical interface or a web interface in the future.
- Regularly update `stateSalesTaxRates.json` to keep the sales tax rates current.
- Implement additional error handling and logging for improved robustness.

## Contributing

Contributions are welcome! If you find any issues or have suggestions for improvements, please open an issue or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

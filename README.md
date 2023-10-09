# Invoice Generator

The Invoice Generator is a user-friendly console application that allows you to generate, store, and manage invoices. With its seamless integration with MongoDB hosted on Azure Cosmos DB and Azure Blob Storage, users can not only create invoices but also save them as PDFs and fetch them as needed. It calculates the sales tax based on the state code provided, and the PDF invoices are stored in Azure Blob Storage with anonymous access. The public URL of each invoice is also stored in the MongoDB database.

## Getting Started

### Prerequisites

- .NET Core SDK installed.

- MongoDB server.

- Azure Blob Storage account with a container for storing the PDF invoices.

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


> **Note:** For users with a remote MongoDB server, update the connection string in `appsettings.json` with the appropriate connection string for your server.

### Get Started with Azure Cosmos DB for MongoDB

You can sign up for Azure Cosmos DB for MongoDB for free and start building scalable applications. To get started, visit [https://aka.ms/trycosmosdb](https://aka.ms/trycosmosdb) to create a free account and explore the powerful features of Azure Cosmos DB.

## How to Use

1. Clone the repository.
2. Navigate to the project directory in your terminal.
3. Start the application with:

   ```shell
    dotnet run
    ```

4. Follow the prompts to interact with the Invoice Generator.

5. Use the interactive menu to create, delete, undelete, and view the invoices.

## Application Components

### Overview

A simple yet robust invoice generator that leverages MongoDB for storing invoice data and Azure Blob Storage for keeping the invoice PDFs.

### Configuration

- `appsettings.json`: Contains connection strings and other configuration essentials.

### Core Components

1. **DBStorage.cs**: Handles all database and storage related operations. It interacts with MongoDB for CRUD operations, Azure Blob Storage for PDF storage, and initializes the database and blob storage.

2. **Invoice.cs**: Contains the `Invoice` model and its associated functionalities such as generating a PDF, adding, deleting, and undeleting invoices.

3. **Item.cs**: Represents individual items within an invoice.

4. **Program.cs (InvoiceGenerator)**: The primary interaction point for the user. It uses the `Sharprompt` library for an intuitive command-line experience.

### Additional Data

- `stateSalesTaxRates.json`: A file with sales tax rates specific to U.S. states and territories.

## Features

- **Generating Invoices**: User-friendly prompts let users input essential details, saving the invoice to MongoDB and its PDF counterpart to Azure Blob Storage.

- **Listing and Managing Invoices**: View a list of existing invoices and manage them with options to delete or undelete.

- **Dynamic Sales Tax Rates**: Depending on the given U.S. state, the application applies the correct sales tax rate from `stateSalesTaxRates.json`.

## Enhancements and Suggestions

- While the application offers a command-line interface, future iterations could introduce a graphical or web interface.

- Ensure `stateSalesTaxRates.json` is updated regularly to maintain current tax rates.

- Further error handling and logging mechanisms can be integrated for increased reliability.

## Contributing

We welcome contributions! If you encounter any bugs or have suggestions, please file an issue in the GitHub project or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

Happy Invoicing! 🧾
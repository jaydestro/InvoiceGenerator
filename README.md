# Invoice Generator

[![build and test](https://github.com/jaydestro/InvoiceGenerator/actions/workflows/build.and-test.yml/badge.svg?branch=main)](https://github.com/jaydestro/InvoiceGenerator/actions/workflows/build.and-test.yml)

## Table of Contents

1. [Invoice Generator](#invoice-generator)
2. [Usage](#usage)
3. [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Get Started with Azure Cosmos DB for MongoDB](#get-started-with-azure-cosmos-db-for-mongodb)
4. [How to Use](#how-to-use)
5. [Application Components](#application-components)
   - [Overview](#overview)
   - [Configuration](#configuration)
   - [Core Components](#core-components)
6. [Features](#features)
7. [Contributing](#contributing)
8. [License](#license)
9. [Appendix](#appendix)
   - [Example `appsettings.json` File](#example-appsettingsjson-file)
   - [State Sales Tax Rates](#state-sales-tax-rates)

The Invoice Generator is a user-friendly console application that allows you to generate, store, and manage invoices. With its seamless integration with MongoDB hosted on Azure Cosmos DB and Azure Blob Storage, users can not only create invoices but also save them as PDFs and fetch them as needed. It calculates the sales tax based on the state code provided, and the PDF invoices are stored in Azure Blob Storage with anonymous access. The public URL of each invoice is also stored in the MongoDB database.

## Usage

- Choose options from the command-line menu to add, list, delete, or undelete invoices.
- Enter customer details, item information, and tax-related details as prompted.
- View PDF invoice URLs when available.

## Getting Started

### Prerequisites

- .NET Core SDK installed.

- MongoDB server running on `localhost:27017` or use a remotely hosted service such as [https://learn.microsoft.com/azure/cosmos-db/mongodb/](Azure Cosmos DB for MongoDB.)

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
```

> **Note:** For users with a remote MongoDB server, update the connection string in `appsettings.json` with the appropriate connection string for your server.

### Get Started with Azure Cosmos DB for MongoDB

You can sign up for Azure Cosmos DB for MongoDB for free and start building scalable applications. To get started, visit [https://aka.ms/trycosmosdb](https://aka.ms/trycosmosdb) to create a free account and explore the powerful features of Azure Cosmos DB.

## How to Use

1. Clone the repository to your local machine.

2. Switch to the console application directory

```bash
cd InvoiceGenerator
```

3. Open a command prompt or terminal and navigate to the project directory.

4. Run the following command to start the application:

```shell
    dotnet run
```

5. Use the interactive menu to create, delete, undelete, and view the invoices.

## Application Components

### Overview

A simple yet robust invoice generator that leverages MongoDB for storing invoice data and Azure Blob Storage for keeping the invoice PDFs.

### Configuration

- `appsettings.json`: Contains connection strings and other configuration essentials.

### Core Components

1. **DBStorage.cs**: Handles all database and storage related operations. It interacts with MongoDB for CRUD operations, Azure Blob Storage for PDF storage, and initializes the database and blob storage.

1. **Invoice.cs**: Contains the `Invoice` model and its associated functionalities such as generating a PDF, adding, deleting, and undeleting invoices.

1. **Item.cs**: Represents individual items within an invoice.

1. **Program.cs (InvoiceGenerator)**: The primary interaction point for the user. It uses the `Sharprompt` library for an intuitive command-line experience.

1. `stateSalesTaxRates.json`: A file with sales tax rates specific to U.S. states and territories.

## Features

- **Generating Invoices**: User-friendly prompts let users input essential details, saving the invoice to MongoDB and its PDF counterpart to Azure Blob Storage.

- **Listing and Managing Invoices**: View a list of existing invoices and manage them with options to delete or undelete.

- **Dynamic Sales Tax Rates**: Depending on the given U.S. state, the application applies the correct sales tax rate from `stateSalesTaxRates.json`.

## Contributing

We welcome contributions! If you encounter any bugs or have suggestions, please file an issue in the GitHub project or submit a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

Happy Invoicing! 🧾

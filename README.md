# Invoice Generator

The Invoice Generator is a user-friendly console application designed to generate, store, and manage invoices. Utilizing the power of MongoDB hosted on Azure Cosmos DB and Azure Blob Storage, users can efficiently create invoices, save them as PDFs, and retrieve them with ease. The application also calculates the sales tax based on the provided state code.

## Getting Started

### Prerequisites

- .NET Core SDK.

- A running MongoDB server on `localhost:27017`.

- Azure Blob Storage account and container.

- Properly configured `appsettings.json` with MongoDB and Azure Blob Storage connection strings.

Example `appsettings.json`:

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

> **Note:** Adjust the connection string in `appsettings.json` if using a remote MongoDB server.

### Get Started with Azure Cosmos DB for MongoDB

Sign up for Azure Cosmos DB for MongoDB for free and start building scalable applications. Visit [https://aka.ms/trycosmosdb](https://aka.ms/trycosmosdb) to create a free account and explore the extensive capabilities of Azure Cosmos DB.

## How to Use

1\. Clone the repository.

2\. Navigate to the project directory in your terminal.

3\. Start the application with:

   ```shell

    dotnet run

   ```

4\. Follow the prompts.

5\. Utilize the menu for creating, deleting, undeleting, and viewing invoices.

## Application Components Breakdown

### Configuration

- **appsettings.json**: Central configuration file containing connection strings and other settings.

### Core Components

1\. **DBStorage.cs**: 

    - **Purpose**: Manages database and storage-related operations.

    - **Features**:

        - CRUD operations with MongoDB.

        - Invoice PDF storage in Azure Blob Storage.

        - Initialization of the MongoDB database and blob storage.

2\. **Invoice.cs**:

    - **Purpose**: Represents an invoice and its associated operations.

    - **Features**:

        - PDF generation for invoices.

        - Adding, deleting, and undeleting invoice functionalities.

3\. **Item.cs**: 

    - **Purpose**: Represents individual items that are part of an invoice.

4\. **Program.cs (InvoiceGenerator)**:

    - **Purpose**: Main user interaction hub.

    - **Features**:

        - Uses the `Sharprompt` library for an intuitive user interface.

        - Facilitates invoice creation, listing, and management.

### Additional Data

- **stateSalesTaxRates.json**: A repository of sales tax rates specific to various U.S. states and territories.

## Features and Functionalities

- **Invoice Creation**: Facilitates invoice creation through a series of prompts, saving data to MongoDB and PDFs to Azure Blob Storage.

- **Invoice Management**: Offers listing, deletion, and undeletion of existing invoices.

- **Dynamic Sales Tax Application**: The system fetches and applies the correct sales tax rate based on the provided state code.

## Future Directions and Notes

- Consider migrating from the command-line interface to a more user-friendly GUI or web interface.

- Regular updates to `stateSalesTaxRates.json` ensure the application uses current sales tax rates.

- Implementation of detailed error handling and logging mechanisms can bolster the system's reliability.

## Contributing

We cherish community contributions! If you discover issues or have enhancement ideas, kindly open a GitHub issue or send in a pull request.

## License

Licensed under the [MIT License](LICENSE).

Happy Invoicing! 🧾
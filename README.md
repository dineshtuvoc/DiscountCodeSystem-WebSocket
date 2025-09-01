Discount Code System - Developer Setup Guide
This guide provides the essential steps to get the project running on a local development machine.

1. Prerequisites
Visual Studio 2022 (with the .NET desktop development workload)

.NET 8 SDK

MySQL Server

2. Setup Steps
Step 1: Set Up the Database
Run the following script in your MySQL instance to create the required database and table.

CREATE DATABASE IF NOT EXISTS discount_system;

USE discount_system;

CREATE TABLE IF NOT EXISTS DiscountCodes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Code VARCHAR(8) NOT NULL,
    IsUsed BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UC_Code UNIQUE (Code)
);

Step 2: Configure the Connection String
In the DiscountServer project, open appsettings.json and update the connection string with your MySQL password.

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=discount_system;Uid=root;Pwd=your_password;"
  }
}

Replace your_password with your MySQL root password.

Step 3: Build the Solution
Open the DiscountCodeSystem.sln file in Visual Studio.

Right-click the solution and select Restore NuGet Packages.

Right-click the solution again and select Build Solution.

3. How to Run
In the Solution Explorer, right-click the Solution 'DiscountCodeSystem'.

Select Set Startup Projects....

Choose Multiple startup projects.

Set the "Action" for both DiscountServer and DiscountClient to Start.

Click OK.

Press F5 to launch both the server and the WPF client.

4. Project Structure
The solution contains two projects that need to be run:

DiscountServer: The backend console application that runs the WebSocket server.

DiscountClient: The WPF desktop application that provides the graphical user interface.

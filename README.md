# DiscountCodeSystem

1. Setup and Installation
Follow these steps to get the project running.

Step 1: Set Up the Database
Open your MySQL management tool (e.g., MySQL Workbench).

Run the following SQL script to create the discount_system database and the DiscountCodes table.

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
Open the solution in Visual Studio.

In the DiscountServer project, open the appsettings.json file.

Modify the DefaultConnection string with your MySQL server's credentials (specifically, your password).

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=discount_system;Uid=root;Pwd=your_password;"
  }
}

Note: Replace your_password with your actual MySQL root password.

Step 3: Restore Dependencies and Build
Right-click the solution in the Solution Explorer and select Restore NuGet Packages.

Once the packages are restored, right-click the solution again and select Build Solution.

6. How to Run the Application
The server and the client must be run at the same time.

In the Solution Explorer, right-click the Solution 'DiscountCodeSystem'.

Select Set Startup Projects....

Choose the Multiple startup projects option.

Set the "Action" for both DiscountServer and DiscountClient to Start.

Click OK.

Press F5 or click the green "Start" button.

Two windows will launch: the server's console window and the WPF client application. The client will automatically connect to the server, and you can begin generating and using codes.

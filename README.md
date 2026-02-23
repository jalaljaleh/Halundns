<br />
<div align="center">
  <a href="https://github.com/jalaljaleh/ZamaniCycleAccounting">
    <img src="https://raw.githubusercontent.com/jalaljaleh/ZamanyCyclet/refs/heads/master/ZamanyCyclet/256.ico" alt="Zamani Cycle Logo" width="120" height="120">
  </a>

  <h3 align="center">Zamani Cycle Accounting</h3>

  <p align="center">
    Lightweight Windows Forms Accounting Software for Motorcycle Repair Shops.
    <br />
    Built with .NET Framework 4.8, Entity Framework 6, and SQLite.
    <br /><br />
    <a href="https://github.com/jalaljaleh/ZamaniCycleAccounting">Repository</a>
    ·
    <a href="https://github.com/jalaljaleh/ZamaniCycleAccounting/issues">Report Bug</a>
    ·
    <a href="https://github.com/jalaljaleh/ZamaniCycleAccounting/issues">Request Feature</a>
  </p>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-Framework-4.8-blueviolet?style=flat-square" alt=".NET Framework 4.8">
  <img src="https://img.shields.io/badge/ORM-Entity%20Framework%206-blue?style=flat-square" alt="Entity Framework 6">
  <img src="https://img.shields.io/badge/Database-SQLite-lightgrey?style=flat-square" alt="SQLite">
  <img src="https://img.shields.io/badge/Platform-Windows%207%2B-blue?style=flat-square" alt="Windows 7+">
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License MIT">
</p>

---

# Zamani Cycle Accounting

**Zamani Cycle Accounting** is a lightweight, production-ready desktop accounting system designed specifically for motorcycle repair shops.

The application is built using:

- .NET Framework 4.8  
- Windows Forms  
- Entity Framework 6 (Code-First)  
- SQLite (local file database)  

It is optimized for single-machine environments, low-RAM systems, and Windows 7 compatibility.

---

## Key Features

- Customer and motorcycle management  
- Repair order tracking  
- Invoice and payment management  
- Check management (received & payable)  
- Supplier and parts management  
- Expense tracking  
- Local SQLite database (no SQL Server required)  
- Fast startup, low memory footprint  
- Designed for small repair shops and local businesses  

---

## Architecture Overview

The project follows a clean, lightweight layered architecture:

- Presentation Layer – Windows Forms UI  
- Application Layer – Service abstractions  
- Domain Layer – Core business entities  
- Infrastructure Layer – EF6 + SQLite implementation  
- Data Layer – DbContext configuration  

No heavy enterprise patterns.  
No dependency injection container.  
No unnecessary abstractions.  

Designed for maintainability and long-term evolution.

---

## Technology Stack

- .NET Framework 4.8  
- C# 7.3  
- Entity Framework 6  
- System.Data.SQLite  
- Code-First approach  
- SQLite local database  

Required NuGet packages:
Install-Package EntityFramework
Install-Package System.Data.SQLite
Install-Package System.Data.SQLite.EF6


---

## Database Strategy

- Local SQLite file stored in:
  `Environment.SpecialFolder.CommonApplicationData`
- Automatically created on first run
- Uses Code-First

---

## System Requirements

- Windows 7, 8, 10, or 11
- .NET Framework 4.8 Runtime
- 2GB RAM minimum
- HDD or SSD

---


## Contributing

Contributions are welcome. 
- Fork the repository
- Create your feature branch
- git checkout -b feature/NewFeature
- Commit your changes
- git commit -m "Add NewFeature"
- Push to the branch
- git push origin feature/NewFeature
- Open a Pull Request

## License

This project is licensed under the MIT License.

## Author
Jalal Jaleh
GitHub: https://github.com/jalaljaleh

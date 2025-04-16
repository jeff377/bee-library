# Bee.NET Framework

Bee.NET Framework is a three-tier software architecture designed to accelerate the development of information systems. Built on **NETStandard 2.0**, it provides a solid foundation compatible with various .NET platforms, including .NET Core, and .NET Framework.

The framework is modularized into several components, each targeting specific functionalities to ensure flexibility and scalability.

---

## Key Features

- **Three-tier architecture**: Supports presentation, business logic, and data access layers.
- **Cross-platform compatibility**: Powered by NETStandard 2.0.
- **Modular components**: Focused libraries for caching, database access, business logic, and more.
- **Rapid development**: Simplifies and accelerates the development of information systems.

---

## Components

### 1. **Bee.Base**
Provides core utilities and shared functionality for other components.

### 2. **Bee.Define**
Manages structured data definitions, including system configurations, database schemas, and form layouts.

### 3. **Bee.Cache**
Supporting the caching of defined data and system data. Defined data includes system settings, database settings, form settings, form layouts, and other definition files. System data includes user connections, system parameters, organizational structures, and more.

### 4. **Bee.Db**
Provides a comprehensive library for database access, including query, update, and transaction support.

### 5. **Bee.Business**
Encapsulates reusable backend business logic and rules.

### 6. **Bee.Connect**
Serves as a connector for backend business logic components and external systems.


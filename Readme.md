

WMS Stock Allocation System - Technical Documentation
Project Overview
This is a Warehouse Management System (WMS) focused on intelligent stock allocation for customer orders. The system handles three main scenarios: automatic stock allocation based on priority, order cancellation with stock release, and SKU quantity corrections with automatic reallocation.
Business Problem
When orders come in from the ERP system, the warehouse needs to allocate stock efficiently while respecting business rules like delivery priorities, complete delivery requirements, and locked warehouse locations. The challenge is handling these allocations dynamically when stock levels change or orders get cancelled.
Technical Approach
I built this using ASP.NET Core Web API with Entity Framework Core and SQLite. The architecture follows clean code principles with clear separation between controllers, business logic services, and data access repositories. Each of the three scenarios has its own service class that handles the specific business rules.
The system processes orders by priority (High > Normal > Low) and uses FIFO for same-priority orders. It can split stock from multiple SKUs to fulfill a single line item and automatically finds substitute SKUs when corrections create shortages.
Key Features

Priority-based order allocation with FIFO
Complete delivery enforcement (all-or-nothing allocation)
Locked location handling
Multi-SKU allocation for single line items
Automatic stock release on cancellation
Smart reallocation when SKU quantities are corrected
Full audit trail for all operations

Technical Stack

.NET 8.0 / C#
ASP.NET Core Web API
Entity Framework Core with SQLite
Repository pattern for data access
Async/await throughout for scalability
NUnit for comprehensive unit testing (45+ tests)
FluentAssertions for readable test assertions

Testing Strategy
I wrote unit tests for all three layers: services, repositories, and domain models. Also included integration tests that verify end-to-end workflows and edge case tests for boundary conditions. Used in-memory database for fast, isolated test execution. Test coverage includes happy paths, error cases, and complex scenarios like priority-based allocation across multiple orders.
Database Design
Five main tables: CustomerOrders, OrderLineItems, SKUs, StockAllocations (junction table), and AuditLog. Relationships are properly configured with foreign keys and cascade deletes where appropriate. The schema supports the many-to-many relationship between line items and SKUs through the allocations table.
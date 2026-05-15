# Donora – Humanitarian Impact Tracker

Donora is a full-stack humanitarian management system designed to support non-profit organizations in tracking initiatives, financial contributions, and fund utilization to ensure high levels of transparency and accountability.

## Architecture & Technical Stack

* **Framework:** ASP.NET Core MVC (Model-View-Controller).
* **Database:** SQL Server.
* **Data Access:** ADO.NET with the **Repository Pattern** to decouple business logic from data persistence.
* **Dependency Injection:** Utilized for service registration and controller orchestration.
* **Frontend:** Bootstrap 5, JavaScript (jQuery), HTML5, CSS3.

## Key Technical Features

### 1. Role-Based Access Control (RBAC)

The system implements granular authorization for two distinct user roles:

* **Administrators:** Manage project lifecycles, log expenditures, and monitor organizational finances.


* **Supporters:** Browse active initiatives, contribute funds, and access personal giving history.



### 2. Transactional Financial Management

* **Atomic Contributions:** Uses ADO.NET to process financial donations and update initiative balances as a single unit of work.
* **Budget Validation:** Implements a server-side validation engine that verifies available funds before allowing expenditures, preventing project over-spending.

### 3. Advanced Analytics & Reporting

The backend utilizes complex SQL aggregations to generate monthly impact reports:

* **Sector Performance:** Identifies categories (e.g., Health, Ecology) with the highest engagement.


* **Administrative Red-Flags:** Automatically detects initiatives with no financial activity (contributions or spend) within specific periods.


* **Engagement Profiles:** Aggregates supporter data to track total initiatives funded per profile.



## Database Schema

The relational schema is engineered to maintain a strict audit trail. It links each contribution to a unique reference ID, a specific supporter, and a target initiative.

# GeoBlocker

A .NET Core Web API that manages blocked countries (permanently or temporarily) and validates IP addresses using a third-party geolocation service. This project uses in-memory data structures (no database) for storing blocked countries and logging blocked attempts.

## Table of Contents
1. [Overview](#overview)  
2. [Features](#features)  
3. [Project Structure](#project-structure)  
4. [Installation & Setup](#installation--setup)  
5. [Usage - Endpoints](#usage---endpoints)  
   1. [CountriesController](#countriescontroller)  
   2. [IpController](#ipcontroller)  
   3. [LogsController](#logscontroller)  
6. [Important Note on Local Testing](#important-note-on-local-testing)  
7. [In-Memory Storage Explained](#in-memory-storage-explained)  
8. [Unit Tests](#unit-tests)  
   1. [InMemoryBlockedStoreTests](#inmemoryblockedstoretests)  
   2. [IpApiServiceTests](#ipapiservicetests)  


---

## Overview
This project allows you to:  
• Block countries permanently or temporarily (for a specified duration).  
• Check whether an IP address is coming from a blocked country.  
• Fetch geolocation details from a third-party service (e.g., ipapi.co or IPGeolocation.io).  
• Log all “blocked check” attempts in memory.  
• Clean up expired temporary blocks automatically via a background service.

It is built using .NET Core (8 or higher), uses an in-memory store (ConcurrentDictionary/ConcurrentQueue) to avoid needing a database, and implements concurrency-safe operations for blocking/unblocking.

---

## Features
• Permanent and temporal blocks of countries by ISO code.  
• Search and pagination for blocked countries.  
• Logging of blocked attempts, also with pagination.  
• Handling API rate limits (HTTP 429) with retries.  
• Background service that periodically removes expired temporary blocks.

---

## Project Structure
Below is a high-level view of the solution:

- **GeoBlocker.Domain**  
  - Entities (BlockedCountry, TemporalBlock, BlockedAttempt)

- **GeoBlocker.Application**  
  - Interfaces (IBlockedStore, IGeoService)  
  - Models (GeoResult, BlockedCountryDetails, TemporalBlockRequest)  
  - Services (e.g., BlockCountryService)

- **GeoBlocker.Infrastructure**  
  - Repositories (InMemoryBlockedStore)  
  - Geolocation (IpApiService)  
  - BackgroundServices (TemporalBlockCleanupService)

- **GeoBlocker.Web (or .API)**  
  - Controllers (CountriesController, IpController, LogsController)  
  - *Program.cs* (startup configuration, DI container setup)

- **GeoBlocker.Tests (Test project)**  
  - InMemoryBlockedStoreTests  
  - IpApiServiceTests  
  - (Any additional unit or integration tests)

---

## Installation & Setup
1. Clone this repository.  
2. Navigate to the project folder containing the .sln (solution) file.  
3. Ensure you have .NET 8 or higher installed.  
4. (Optional) Update *appsettings.json* with your chosen IP geolocation provider API key under the “IpApi” section. For example:

    ```json
    {
      "IpApi": {
        "BaseUrl": "https://api.ipapi.co",
        "ApiKey": "YOUR_API_KEY"
      }
    }
    ```

5. Run:

    ```bash
    dotnet restore
    dotnet build
    ```

6. To start the service (in Development mode with Swagger):

    ```bash
    dotnet run --project GeoBlocker.Web
    ```

   Swagger UI should be available at `https://localhost:<PORT>/swagger`.

---

## Usage - Endpoints
Below is a summary of the main endpoints:

### CountriesController
Base Route: `/api/countries`

1. **POST** `/api/countries/block`  
   - Query Parameter: `code=US`  
   - Permanently block a country by ISO code. Returns **201 Created** if successful or **409 Conflict** if already blocked.

2. **DELETE** `/api/countries/block/{countryCode}`  
   - Remove a country from both permanent and temporal blocks. Returns **204 No Content** if successful, or **404 Not Found** if not found in either list.

3. **GET** `/api/countries/blocked`  
   - Optional Query Parameters:  
     - `search` (filter by code or name)  
     - `page` (default=1)  
     - `pageSize` (default=10)  
   - Returns a paginated list of both permanently and temporarily blocked countries, along with remaining minutes for temporary blocks.

4. **POST** `/api/countries/temporal-block`  
   - Body:
   
        ```json
        {
          "CountryCode": "EG",
          "DurationMinutes": 120
        }
        ```
   
   - Temporarily block a country for a specified duration (1 to 1440 minutes). Returns **200 OK** if successful or **409 Conflict** if already blocked.

### IpController
Base Route: `/api/ip`

1. **GET** `/api/ip/lookup?ipAddress={ip}`  
   - If `ipAddress` is omitted, it uses the caller’s IP (via `X-Forwarded-For` or `RemoteIpAddress`).  
   - Returns geolocation details (country code, name, etc.) from the third-party API, or **502 Bad Gateway** if the lookup fails.

2. **GET** `/api/ip/check-block`  
   - Uses the caller’s IP.  
   - Looks up the country, checks if it’s blocked, logs the attempt.  
   - Returns a JSON object, for example:
   
     ```json
     {
       "ip": "1.2.3.4",
       "country": "US",
       "isBlocked": true
     }
     ```

### LogsController
Base Route: `/api/logs`

1. **GET** `/api/logs/blocked-attempts`  
   - Optional Query Parameters: `page` (default=1), `pageSize` (default=20)  
   - Returns a paginated list of all block check attempts (time, IP, country code, user agent, whether blocked, etc.).

---

## Important Note on Local Testing
When developing or running this application on a local machine, any code that relies on “GetCallerIp()” will often receive “::1” as the caller IP (the IPv6 loopback address). The external geolocation API cannot resolve “::1” as a valid public IP, so two key endpoints may fail or return null data:

• “/api/ip/check-block” (automatically uses GetCallerIp)  
• “/api/ip/lookup” if ipAddress is omitted (also calls GetCallerIp)

Because “::1” is not recognized by the geolocation service, you’ll likely see errors or null responses when calling these endpoints locally without specifying a real external IP. To properly test them, you can either:  
1. Deploy to an environment that provides a real IP address, or  
2. Pass a known external IP as a query param (for example, /api/ip/lookup?ipAddress=8.8.8.8) to confirm the geolocation logic works as intended.

---

## In-Memory Storage Explained
The project intentionally avoids a database. Instead, it uses thread-safe collections:

- A `ConcurrentDictionary<string, BlockedCountry>` for permanently blocked countries.  
- A `ConcurrentDictionary<string, TemporalBlock>` for temporarily blocked countries (with an expiration).  
- A `ConcurrentQueue<BlockedAttempt>` for logging all attempts at IP blocking checks.

### Automatic Cleanup of Temporal Blocks
- A background service (`TemporalBlockCleanupService`) runs every 5 minutes.  
- It calls `RemoveExpiredTemporal()` on the `InMemoryBlockedStore`, removing any entries whose expiration is in the past.

---

## Unit Tests
A separate test project **GeoBlocker.Tests** contain examples of how to test key parts of this application.

### InMemoryBlockedStoreTests
Located in `GeoBlocker.Tests/InMemoryBlockedStoreTests.cs`, these tests cover:
- Adding permanent blocks.  
- Blocking a country that’s already blocked.  
- Removing permanent blocks.  
- Adding & removing temporary blocks (with expiry).  
- Checking “IsBlocked” logic.  
- Retrieving merged lists of permanently and temporarily blocked countries.

### IpApiServiceTests
Located in `GeoBlocker.Tests/IpApiServiceTests.cs`, these tests demonstrate:
- Mocking `HttpClient` to simulate various HTTP responses (200 OK, 400 Bad Request, 429 Rate Limit).  
- Verifying success parsing JSON into `GeoResult`.  
- Handling 429 (Too Many Requests) with automatic retries, then returning success or null.  
- Handling non-success status codes by returning null.

#### How to Run Tests
1. Navigate to the solution root (where the .sln file is).  
2. Run:

    ```bash
    dotnet test
    ```

   This will build both the main projects and the tests, then execute all test methods.

---

# Chat Room Challenge
This project is a browser-based chat application built with **.NET** and **React**.
Users can chat in real time and request stock quotes using a command-based syntax.

Example:

```
/stock=aapl.us
```

A **decoupled bot worker** consumes stock commands from **RabbitMQ**, calls the **Stooq** API (CSV response), parses the stock quote, and publishes the result back into the chat as **StockBot**.

---

## Features

* User registration and login.
* Real-time chat using SignalR.
* Command-based stock quotes using `/stock=STOCK_CODE`.
* Decoupled bot using RabbitMQ.
* Stock quotes retrieved from Stooq (CSV parsing).
* Messages ordered by timestamp.
* Only the last **50 messages** are shown.
* Unit tests for selected deterministic logic.
* Multiple chatrooms.
* ASP.NET Identity authentication.
* Graceful handling of invalid stock codes.

---

## Tech Stack

* **Backend:** .NET, Minimal APIs, ASP.NET Identity, EF Core (SQLite)
* **Realtime:** SignalR
* **Frontend:** React + Vite
* **Messaging:** RabbitMQ
* **Bot Worker:** .NET Worker Service
* **External API:** Stooq (CSV)

---

## Local Development

### Prerequisites

* .NET SDK (built in .NET 9)
* Node.js + npm
* Docker (for RabbitMQ)

---

## 1) Start RabbitMQ

Using Docker:

```bash
docker compose up -d
```

RabbitMQ Management UI:

* [http://localhost:15672](http://localhost:15672)
  Credentials: `guest / guest`

---

## 2) Start the Backend (ChatRoom.Api)

### Database migration (first time only)
```bash

```md
```bash
dotnet ef database update --project src/ChatRoom.Api


```bash
dotnet run --project src/ChatRoom.Api
```

### Important Local Notes

* API runs on: **[http://localhost:5000](http://localhost:5000)**
* Cookie-based authentication uses:

  ```
  POST /login?useCookies=true
  ```

This is required for:

* curl testing
* React authentication
* SignalR authentication

---

## 3) Start the Stock Bot Worker

```bash
dotnet run --project src/ChatRoom.StockBotWorker
```

---

## 4) Start the React Frontend

```bash
cd chatroom-ui
npm install
npm run dev
```

Open:

* [http://localhost:5173](http://localhost:5173)

> The React app uses a Vite proxy to forward `/api`, `/login`, `/register`, `/logout`, and `/hubs` to the backend.

---

## Usage

### Register & Login

* Register a user in the UI.
* Open **two browser windows** (or one incognito) and log in with different users.

### Chat

* Join or create a chatroom.
* Messages are delivered in real time.
* Only the last 50 messages are displayed.

### Stock Command

Send:

```
/stock=aapl.us
```

Example response:

```
AAPL.US quote is $255.53 per share
```

### Important Behavior

* The `/stock=...` command is **not persisted** in the database.
* Only the bot response is stored and displayed.

---

## Testing

Run unit tests:

```bash
dotnet test
```

Tests cover:

* Stock command parsing.
* CSV parsing of the Stooq response.

---

## Architecture Overview

### Components

* **React UI:** simple interface for auth and chat.
* **ChatRoom.Api:**

  * Authentication and persistence.
  * REST endpoints for rooms and message history.
  * SignalR hub for real-time messaging.
  * Publishes stock commands to RabbitMQ.
  * Consumes stock results and posts bot messages.
* **RabbitMQ:**

  * `stock.commands` queue for stock requests.
  * `stock.results` queue for processed results.
* **ChatRoom.StockBotWorker:**

  * Consumes commands.
  * Calls Stooq API.
  * Parses CSV data.
  * Publishes stock results.
* **ChatRoom.Shared:**

  * Shared contracts and pure parsing logic used by ChatApi and the worker.

### Runtime Flow

1. User logs in using cookie-based authentication.
2. Normal messages are saved and broadcast via SignalR.
3. `/stock=CODE` is detected and published to RabbitMQ.
4. The bot worker consumes the command and retrieves the stock quote.
5. The result is published back to RabbitMQ.
6. ChatApi consumes the result and broadcasts it as **StockBot**.

---

## Architecture Tradeoffs & Improvements

* SQLite is used for fast local setup; Postgres would be a natural production choice.
* For a larger system, improvements could include:

  * Application layer with use-case handlers.
  * CQRS for complex domains.
  * Outbox pattern for message reliability.
  * Retries, backoff strategies, and dead-letter queues.
  * Idempotency handling for broker messages.

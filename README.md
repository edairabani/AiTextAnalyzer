AI RAG System – .NET + OpenAI + pgvector
Enterprise-grade Retrieval-Augmented Generation (RAG) system built with modern .NET technologies and a React frontend.

Overview
This project demonstrates how to build a production-ready AI-powered backend using:

- .NET 8 Web API
- OpenAI (LLM + Embeddings)
- PostgreSQL with pgvector
- JWT Authentication
- Rate Limiting
- Docker Compose
- React UI (Vite)

It includes semantic search, document ingestion, citation-backed answers, and a secured API architecture.

Architecture:

User (React UI)
        ↓
.NET 8 Web API (JWT secured)
        ↓
OpenAI (LLM + Embeddings)
        ↓
PostgreSQL (pgvector)

Flow:
1. Documents are ingested and chunked.
2.Embeddings are generated using OpenAI.
3.Chunks are stored in PostgreSQL with vector embeddings.
4.User question → embedding → semantic search.
5.Top chunks are injected into LLM prompt (RAG).
6.Answer returned with citations and distance score.

Features

    AI & RAG
    - Document ingestion & chunking
    - OpenAI embeddings
    - pgvector semantic search
    - Threshold-based retrieval
    - Citation-backed answers
    - Distance scoring
    - Language auto-detection (DE/EN)

    Security
    - JWT Authentication
    - Role-based authorization
    - Per-user rate limiting

    Infrastructure
    - Dockerized backend
    - PostgreSQL + pgvector container
    - Environment variable configuration
    - React frontend (Vite)

Tech Stack
    Backend
    - .NET 8
    - ASP.NET Core
    - Entity Framework Core
    - PostgreSQL
    - pgvector
    - OpenAI API

    Frontend
    - React
    - Vite
    - DevOps

    Docker
    - Docker Compose

Running the Project
1️.Configure environment

    Create a .env file:
        OPENAI_API_KEY=your_openai_key
        JWT_KEY=your_super_secret_key

2️. Start everything
    docker compose up --build

    Backend:
        http://localhost:8080/swagger

    Frontend:
        http://localhost:5173

    Demo Login
        Username: admin
        Password: password


Example Response
{
  "answer": "Users can reset their password via the settings page.",
  "citations": [
    {
      "id": 1,
      "snippet": "The password can be reset via the settings page.",
      "distance": 0.41
    }
  ]
}

What This Project Demonstrates
    - Production-ready RAG architecture
    - Vector similarity search in PostgreSQL
    - Hallucination mitigation via threshold filtering
    - Secure AI API design
    - Cost-control via rate limiting
    - Containerized deployment

Future Improvements
    - Redis caching
    - Structured logging (Serilog)
    - Background ingestion queue
    - Multi-user support
    - Admin dashboard

Why This Matters
This project shows how to build enterprise AI systems using modern .NET technologies — beyond simple AI demos.


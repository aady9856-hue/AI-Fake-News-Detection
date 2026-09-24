# AI-Based Fake News Detection System

An ASP.NET Core MVC web application that uses AI-assisted analysis to classify news articles as **REAL, FAKE, or UNCERTAIN**.

## Project Overview

The AI-Based Fake News Detection System is designed to help users analyze news content and identify potentially misleading or fabricated information.

The system allows users to:

- Enter news manually
- Analyze news using a URL
- Get AI-assisted prediction
- View confidence percentage
- View explanation and indicators
- Save analysis history
- View dashboard statistics
- Generate PDF reports

> **Disclaimer:** This application provides AI-assisted analysis and does not guarantee that a news article is factually true or false. Important claims should be verified using reliable sources.

## Features

- User Registration and Login
- Manual News Analysis
- URL-Based News Analysis
- AI-Assisted Fake News Detection
- REAL / FAKE / UNCERTAIN Prediction
- Confidence Percentage
- Explanation of Prediction
- News Indicators
- Analysis History
- User-Specific History
- Delete Analysis
- Dashboard
- GNews API Integration
- PDF Report Generation
- Form Validation
- Error Handling
- Responsive User Interface
- Dark Mode
- SQL Server Database
- Entity Framework Core

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Programming Language |
| ASP.NET Core MVC | Backend/Web Framework |
| .NET 10 | Application Framework |
| HTML | Web Structure |
| CSS | Styling |
| Bootstrap | Responsive UI |
| JavaScript | Client-side functionality |
| SQL Server | Database |
| Entity Framework Core | ORM |
| ASP.NET Core Identity | Authentication |
| OpenAI API | AI Analysis |
| GNews API | News Search |
| HtmlAgilityPack | Article Extraction |
| Git | Version Control |
| GitHub | Source Code Repository |
| Visual Studio Code | Development Environment |

## Project Architecture

```text
User
 |
 v
Login / Register
 |
 v
News Analysis Page
 |
 +----------------------+
 |                      |
Manual News          News URL
 |                      |
 |                 Article Extraction
 |                      |
 +----------+-----------+
            |
            v
     AI Analysis Service
            |
            v
 REAL / FAKE / UNCERTAIN
            |
            +---- Confidence
            |
            +---- Explanation
            |
            +---- Indicators
            |
            v
      SQL Server Database
            |
       +----+----+
       |         |
       v         v
    Result    History
                |
                v
             Dashboard
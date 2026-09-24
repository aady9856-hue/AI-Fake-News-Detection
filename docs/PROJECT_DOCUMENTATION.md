# AI-Based Fake News Detection System
## Project Documentation

## 1. Introduction

The AI-Based Fake News Detection System is a web application developed using ASP.NET Core MVC. Its purpose is to assist users in evaluating news content by analyzing the language and information provided in a news article.

The application accepts news content manually or extracts article content from a supplied URL. The extracted text is processed by an AI service, which returns a classification, confidence value, explanation, and indicators.

## 2. Problem Statement

The rapid spread of information through digital platforms makes it difficult for users to distinguish reliable news from fabricated or misleading content. Manual verification can be time-consuming.

The proposed system provides an AI-assisted first-level analysis that helps users identify potentially suspicious news content.

## 3. Objectives

- Provide an easy-to-use news analysis interface.
- Accept headline and article content.
- Support URL-based article extraction.
- Use an AI service for content analysis.
- Display prediction and confidence.
- Explain the factors identified during analysis.
- Store analysis history in a database.
- Provide dashboard statistics.
- Generate PDF reports.
- Implement authentication and user-specific records.

## 4. Functional Requirements

### User Management

- Register a new account.
- Log in and log out.
- Restrict user-specific history to authenticated users.

### News Analysis

- Enter headline and article content.
- Submit an article URL.
- Validate user input.
- Analyze news using the AI service.
- Display prediction, confidence, explanation, and indicators.

### History

- Save completed analyses.
- Display previous analyses.
- Delete an analysis when permitted.

### Dashboard

- Display analysis statistics.
- Provide an overview of stored predictions.

### Reporting

- Generate a PDF report for analysis results.

## 5. Non-Functional Requirements

- **Usability:** Simple and responsive interface.
- **Performance:** Asynchronous API/database operations where applicable.
- **Security:** Credentials and API keys must not be committed to source control.
- **Reliability:** Service failures should use safe fallback behavior.
- **Maintainability:** Application logic is separated into controllers, models, data, and services.
- **Scalability:** Service-based design allows individual components to be extended.

## 6. System Architecture

The application follows an MVC-based layered structure:

```text
Presentation Layer
    Views + HTML/CSS/Bootstrap/JavaScript
              |
              v
Controller Layer
    HomeController
    NewsController
    DashboardController
              |
              v
Service Layer
    FakeNewsDetectionService
    AIService
    NewsUrlService
    NewsSearchService
    PdfReportService
              |
       +------+------+
       |             |
       v             v
 OpenAI API      SQL Server
                     ^
                     |
             Entity Framework Core
# Identity Samples

## Introduction

This repository contains a Visual Studio solution that demonstrates modern claims-based identity scenarios for .NET developers, with a particular focus on authentication and authorization using [Azure Active Directory](http://azure.microsoft.com/en-us/services/active-directory/) and/or [Windows Server Active Directory Federation Services](https://technet.microsoft.com/library/hh831502.aspx).

It is based on the [official Azure Active Directory samples repository](https://github.com/AzureADSamples), but with the main difference that it contains a single solution to show multiple integrated scenarios instead of having multiple separate solutions for each scenario.

**IMPORTANT NOTE: The code in this repository is _not_ production-ready. It serves only to demonstrate the main points via minimal working code, and contains no exception handling or other special cases. Refer to the official documentation and samples for more information. Similarly, by design, it does not implement any caching or data persistence (e.g. to a database) to minimize the concepts and technologies being used.**

## Scenario

* There is a _Todo List_ service which stores simple todo items for users. A todo item has a _title_ and a _category_.
* Categories can be either _public_ (for all users) or _private_ (only for the user that created it) and are maintained in a separate _Taxonomy_ service (to show delegated on-behalf-of access from one service to another).
* The Todo List service can be accessed via a number of client applications.

```
Client 1 --\
Client 2 ---\
...          > ---> Todo List Service ---> Taxonomy Service
Client n ---/
```

## Implementation

| Project | Purpose | Technology | Protocol |
|---------|---------|------------|----------|
| TaxonomyWebApi | Taxonomy service | ASP.NET Web API | OAuth 2.0 Bearer Tokens |
| TodoListWebApi | Todo List service | ASP.NET Web API | OAuth 2.0 Bearer Tokens; OAuth 2.0 On-Behalf-Of |
| TodoListWebApp | Server-side web application | ASP.NET MVC | OpenID Connect; OAuth 2.0 Authorization Code Grant, Confidential Client |
| TodoListWebSpa | Client-side Single Page Application (SPA) | AngularJS | OAuth 2.0 Implicit Grant |
| TodoListWpf | Windows desktop application | WPF | OAuth 2.0 Authorization Code Grant, Public Client |
| TodoListConsole | Windows desktop application | Console | OAuth 2.0 Authorization Code Grant, Public Client |
| TodoListDaemon | Non-interactive daemon service | Console | OAuth 2.0 Client Credential Grant; Confidential Client with Certificate authentication |
| TodoListUniversal.Windows | Windows Store application | Universal App | OAuth 2.0 Authorization Code Grant, Public Client |
| TodoListUniversal.WindowsPhone | Windows Phone application | Universal App | OAuth 2.0 Authorization Code Grant, Public Client |

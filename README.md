# Introduction

This project is a basic web application built with ASP.NET Core MVC using .NET 8. It follows the Model-View-Controller (MVC) architectural pattern, which is a standard design pattern that many developers are familiar with. It provides a clean and organized structure for your project, making it easier to maintain and scale.

This project is for Web Application Development.

# Getting Started

## Prerequisites

- .NET 8 SDK
- Visual Studio 2019 or later

## Installation

1. Clone the repository

```
git clone https://FutureIRAS@dev.azure.com/FutureIRAS/Research%20Systems%20Programme/_git/rsp-iras-portal
```
2. Navigate to the project directory

```
cd rsp-iras-portal
```

3. Restore the packages
```
dotnet restore
```
# Build and Test

1. To build the project, navigate to the project directory and run the following command:

```
dotnet build
```

2. To run the tests, use the following command. Path to the test project is needed if you are running the tests from outside the test project directory.

```
 dotnet test .\tests\UnitTests\Rsp.IrasPortal.UnitTests\ --no-build

 dotnet test .\tests\IntegrationTests\Rsp.IrasPortal.IntegrationTests\ --no-build
```

3. To run the application, use the following command:

```
dotnet run --project .\src\Web\Rsp.IrasPortal.Web\
```

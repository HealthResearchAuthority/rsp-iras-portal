# Introduction

This project is a basic web application built with ASP.NET Core MVC using .NET9. It follows the Model-View-Controller (MVC) architectural pattern, which is a standard design pattern that many developers are familiar with. It provides a clean and organized structure for your project, making it easier to maintain and scale.

This project is for Web Application Development.

# Contributing

For detailed instructions on how to contribute to this project, please read [CONTRIBUTING.md](./docs/CONTRIBUTING.md) 

# Getting Started

## Prerequisites

- .NET 9 SDK
- VsCode or Visual Studio 2022 or later
- Configure secrets (sensitive data) using Secret Manager tool

## User Secrets

The applicaition is integrated with the IDG (Identity Gateway) and requiers ClientId and ClientSecret. These are senstive values and shouldn't be committed to the source code. A more secure approach to store secrets in your development environment is to use the Secret Manager. For other environments, the secrets will be stored in the Azure KeyVault. It can also be configured for Dev.

Secret Manager tool is available as a CLI command. It is also integrated into Visual Studio: you can right-click the project in the Solution Explorer and select the Manage User Secrets item from the context menu.

If this is a brand new project, The first step you need to do is to enable your project to use the Secret Manager. You can do so by running the following command in the root folder of your project:

```
dotnet user-secrets init --project "<project path>"
```
### Note: You only needed the above if you created a new project.

After the initialization or if it's an existing project, you can store your application secrets by using the dotnet user-secrets set command. For example, to store the ClientId and ClientSecret, you can run the following commands:

```
dotnet user-secrets set "AppSettings:ClientId" "<YOUR-CLIENTID>"
dotnet user-secrets set "AppSettings:ClientSecret" "<YOUR-CLIENT-SECRET>"
```
If you don't want to use the CLI command, you can use the Visual Studio's built-in option and directly add the secrets to the file. You can right-click the project in the Solution Explorer and select the Manage User Secrets item from the context menu. It will open the json file where you can add the secrets like below. Please note that it's a flat json file, so Key/Value needs to be on separate lines separated by :

```json
{
  "AppSettings:ClientId": "<client id>"
  "AppSettings:ClientSecret": "<client secret>"
}
```
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
dotnet run --project .\src\AppHost\Rsp.IrasPortal\
```
# License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details. Please see [HRA's Licensing Terms](https://dev.azure.com/FutureIRAS/Research%20Systems%20Programme/_wiki/wikis/RSP.wiki/84/Licensing-Information) for more details.

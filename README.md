# DataForSeo Clients Generator

A OpenAPI-based code generator that creates client libraries for DataForSeo API in multiple programming languages.

## Overview

DataForSeo Clients Generator is a .NET-based tool that automatically generates client SDK libraries for the DataForSeo API using OpenAPI specifications. It supports multiple programming languages and provides type-safe, well-documented client libraries that make it easy to integrate with DataForSeo services.

## Supported Languages

- **C# (.NET)**
- **Java**
- **TypeScript**
- **Python**

## Getting Started

The easiest way to generate client libraries is through the **Console application** (`OpenApiGenerator.Console`), which serves as the main entry point for the code generator.

### Command Line Options

The Console application supports the following command-line options:

```bash
cd OpenApiGenerator.Console
dotnet run -c Release -- [options]
```

**Available Options:**

- `--languages` - Programming languages to generate code for (comma-separated)
  - Available: `CSharp`, `Java`, `Python`, `TypeScript`
  - Default: All languages
  - Example: `--languages CSharp Python`

- `--doc` - Path to the OpenAPI YAML specification file
  - Default: Downloads from official DataForSeo GitHub [repository](https://github.com/dataforseo/OpenApiDocumentation)
  - Example: `--doc C:\path\to\openapi.yaml`

- `--out` - Output root path for generated client libraries
  - Default: Executing directory
  - Example: `--out C:\Generated\Clients`

- `--login` - DataForSeo API username for configuration test classes to fetch data from api
  - Default: username
  - Example: `--login your-username`

- `--password` - DataForSeo API password for test configuration test classes to fetch data from api
  - Default: password
  - Example: `--password your-password`

### Usage Examples

**Generate all client libraries with default settings:**
```bash
dotnet run -c Release
```

**Generate only C# and Python clients:**
```bash
dotnet run -c Release -- --languages CSharp Python --out "D:\MyClients"
```

**Generate with custom OpenAPI file and credentials:**
```bash
dotnet run -c Release -- --doc "D:\api\openapi.yaml" --login "myuser" --password "mypass" --out "C:\Output"
```

The generator will automatically create appropriate project structures, dependencies, and test configurations for each selected language.

## License

This project is licensed under the [MIT License](LICENSE).

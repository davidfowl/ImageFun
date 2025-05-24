# ImageFun 📸✨

ImageFun is an AI-enabled photo gallery application that allows users to upload, view, and automatically generate *fun* descriptions for their images. Built with modern .NET technologies and powered by OpenAI's GPT-4o model, ImageFun combines cloud storage with artificial intelligence to create an enhanced photo management experience.

## Architecture

ImageFun is built as a distributed application using .NET Aspire with two main components:

### Frontend (ImageUpload)
- **Technology**: ASP.NET Core with Razor Components
- **Purpose**: Serves the web UI, handles image uploads, and manages the photo gallery
- **Deployment**: Azure App Service Environment

### Backend (ImageProcessor)
- **Technology**: ASP.NET Minimal API
- **Purpose**: Processes images and generates AI descriptions using OpenAI
- **Deployment**: Azure Container Apps Environment

## External Dependencies

To run ImageFun, you'll need the following external services:

### Required Services

1. **OpenAI API Key** (`oaikey`)
   - Used for generating AI descriptions of uploaded images
   - Requires a valid OpenAI API key with access to GPT-4o model
   - Configured as a secret parameter in the Aspire host (`Parameters:oaikey`)

## .NET Aspire 9.3 Compute Environments Showcase

This project showcases the new **Compute Environments** feature introduced in .NET Aspire 9.3, which allows you to deploy different parts of your application to different Azure compute services based on their specific requirements.

### Compute Environment Configuration

```csharp
// Frontend - optimized for web serving
var feenv = builder.AddAzureAppServiceEnvironment("fe-env")
    .WithAzureContainerRegistry(acr);

// Backend - optimized for API processing
var beenv = builder.AddAzureContainerAppEnvironment("be-env")
    .WithAzureContainerRegistry(acr);

// Assign services to appropriate environments
var imageProcessor = builder.AddProject<Projects.ImageProcessor>("imageprocessor")
    .WithComputeEnvironment(beenv);  // API service → Container Apps

builder.AddProject<Projects.ImageUpload>("web")
    .WithComputeEnvironment(feenv);  // Web frontend → App Service
```

### Why Different Compute Environments?

This application is showcasing using different compute environments for different workloads:

**App Service Environment (Frontend)**
- Optimized for web applications with built-in scaling
- Excellent for serving static content and web UIs
- Integrated deployment and monitoring capabilities
- Cost-effective for web workloads

**Container Apps Environment (Backend)**
- Perfect for microservices and API workloads
- Event-driven scaling capabilities
- Better resource utilization for processing tasks
- Ideal for AI/ML workloads that may have variable demand

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Azure CLI
- Docker Desktop (for local development)
- OpenAI API key

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ImageFun
   ```

2. **Set up user secrets for OpenAI**
   ```bash
   cd ImageFun.AppHost
   dotnet user-secrets set "Parameters:oaikey" "your-openai-api-key-here"
   ```

3. **Install the aspire cli**
   ```bash
    dotnet tool install -g aspire --prerelease
   ```

4. **Run the application**
   ```bash
   aspire run
   ```

5. **Access the application**
   - Open your browser to the URL shown in the Aspire dashboard
   - Upload images and enjoy AI-generated descriptions!

### Deployment to Azure

1. **Provision Azure resources**
   ```bash
   azd up
   ```

## Technology Stack

- **.NET 9.0**: Latest .NET runtime and SDK
- **ASP.NET Core**: Web framework for both frontend and backend
- **Razor Components**: Server-side rendering for the UI
- **.NET Aspire 9.3**: Cloud-native orchestration and deployment
- **Azure Blob Storage**: Image storage and retrieval
- **OpenAI GPT-4o**: AI-powered image description generation
- **Azure App Service**: Frontend hosting
- **Azure Container Apps**: Backend API hosting

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test locally using the Aspire dashboard
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

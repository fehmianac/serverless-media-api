
# Serverless Media API

## Overview

The Serverless Media API is a project developed using .NET 6, Amazon DynamoDB, Amazon S3, Amazon SNS, and AWS Lambda. It provides functionalities for file upload to S3 with PresignURL, serving images from S3, and creating/managing galleries.

## Infrastructure Diagram
![media-api-infra.png](docs%2Fmedia-api-infra.png)

## Features

1. **File Upload to S3 with PresignURL:**
   - Uploading files to Amazon S3 with pre-signed URLs for secure and efficient file transfers.

2. **Serve Image from S3:**
   - Serving images directly from Amazon S3 for improved performance and scalability.

3. **Create and Manage Gallery:**
   - Functionality to create and manage galleries for organizing and displaying media content.

## Getting Started

### Prerequisites

- [.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Amazon DynamoDB](https://aws.amazon.com/dynamodb/)
- [Amazon S3](https://aws.amazon.com/s3/)
- [Amazon SNS](https://aws.amazon.com/sns/)
- [AWS Lambda](https://aws.amazon.com/lambda/)

### Deployment

1. Clone the repository:
   ```bash
   git clone https://github.com/fehmianac/serverless-media-api
   ```

2. Navigate to the project directory:
   ```bash
   cd serverless-media-api
   ```

3. Use the CloudFormation template `template.yaml` for deployment:
   ```bash
   aws cloudformation deploy --template-file template.yaml --stack-name ServerlessMediaApiStack --capabilities CAPABILITY_IAM
   ```

## CloudFormation Template

The CloudFormation template (`template.yaml`) in the source repository defines the infrastructure required for the Serverless Media API.

## Usage
The Serverless User API provides the following endpoints:

![media-api-referance.png](docs%2Fmedia-api-referance.png)

## Contact

For any inquiries or assistance, please contact:

**Fehmi Anaç**  
Email: fehmianac@gmail.com

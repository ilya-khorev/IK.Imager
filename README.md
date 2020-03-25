# IK.Imager

![](https://github.com/ilya-khorev/IK.Imager/workflows/Build/badge.svg)

Current status: development  
1st beta version: April 2020

When it's finished there will be the following features:  
1) Image uploading (Png, Jpeg, Bmp, Gif) and validation
2) Storing images in Azure File Storage
3) Storing images metadata in Azure CosmosDB
3) Thumbnails generating in the background
4) Ability to retrieve a list of metadata objects for the given set of images, which will also include image urls (+cdn urls)

The system consists of 2 microservices:
1) API. Provides the aforementioned functiolity to the clients. It's recommended to interact with this service from API Gateway, as there is no authorization and it is mostly considered as an internal microservice.
2) Backround service. Used for mostly thumbnails generating right after the original image is uploaded.

Technologies used:
1) Azure Files Storage
2) Azure CosmoDB
3) Azure ServiceBus
4) Asp.Net Core 3.1
5) Docker

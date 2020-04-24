# IK.Imager

![](https://github.com/ilya-khorev/IK.Imager/workflows/Build/badge.svg)

## Functionality
The system allows to easily store and manage image binary data and image metadata, such as size, dimensions, thumbnails, and related tags.

### Image Upload
There are 2 ways to upload an image to the system
1) By a given http/https image URI
![](docs/UploadImageWithUrlRequest.png)

2) Providing a binary data as a part of multipart/form-data
![](docs/UploadImageRequest.png)

### Image Validation
Before the image is saved to the storage, it's being checked for the following image formats: Png, Jpeg, Bmp, Gif.  
Besides, the system verifies the given image's size and dimenesions and compare them with the configuration threshold values.

### Image Thumbnails
Once a new image is uploaded into the system, the background process starts to generate thumbnails, which will subsequently become avaiable to the clients via API. Thumbnails are generated for particular sizes specified in the configuration. Aspect ratio of an image is retained during this process.

### Image Search
A client is able to request a metadata object for any image uploaded earlier, providing an image identifier. 
A metadata object will also contains image url, which leads directly to the image blob storage or CDN (depending on configuration).

![](docs/GetImageRequest.png)

### Image Removal
Image removal is available via a simple API request. The system will clear up all related metadata and thumnails objects.

## Docker images
[ilyakhorev/ik-imager-api](https://hub.docker.com/r/ilyakhorev/ik-imager-api)  
[ilyakhorev/ik-imager-backgroundservice](https://hub.docker.com/r/ilyakhorev/ik-imager-backgroundservice)

## Architecture Overview
![](docs/Architecture.svg)

The application consists of 2 microservices:
1) API microservice.
This microservice takes all responsibility for storing, removing, and searching images and their metadata.
Internally it communicates with Azure Blob Storage for storing image files and with Cosmos DB for storing metadata of images, such as size, dimensions, image type, and image thumbnails.

2) Background microservice. 
Used for thumbnails generating that happens right after the original image is uploaded via API microservice.  
Another key functionality is image removal. When removal request comes to the API, it just clears up the related metadata object, whereas the blob files of the image and its thumbnails are removed later in the background microservice.

Technologies used:
1) Azure Blob Storage - this is where image files are stored.
2) Azure Cosmos DB - used for storing of metadata objects.
3) Azure ServiceBus - used for passing some events from API to the Background Service
4) Asp.Net Core 3.1
5) Docker - both microservices are avalable as docker images on docker hub

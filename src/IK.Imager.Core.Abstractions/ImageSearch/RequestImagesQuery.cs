﻿using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.Abstractions.ImageSearch;

public record RequestImagesQuery(string[] ImageIds, string ImageGroup) : IRequest<ImagesSearchResult>;
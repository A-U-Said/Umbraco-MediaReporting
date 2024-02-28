﻿using MediaReporting.Messages.Query;
using MediaReporting.Messages.View;
using MediaReporting.Specifications;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Text;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace MediaReporting.Helpers
{
    public class MediaReportHelper : IMediaReportHelper
    {
        private readonly IMediaService _mediaService;
        private readonly IUserService _userService;

        public MediaReportHelper(
            IMediaService mediaService,
            IUserService userService
        ) 
        {
            _mediaService = mediaService;
            _userService = userService;
        }


        public IEnumerable<MediaItemView> GetMediaForFolder(IEnumerable<IMedia> media, MediaSizeFilter filter)
        {
            var result = new List<MediaItemView>();

            foreach (var mediaItem in media)
            {
                if (mediaItem.ContentType.Alias == Constants.Conventions.MediaTypes.Folder)
                {
                    var childCount = _mediaService.CountChildren(mediaItem.Id);
                    result.AddRange(GetMediaForFolder(_mediaService.GetPagedChildren(mediaItem.Id, 0, childCount + 10, out long _), filter));
                }
                else
                {
                    var creator = _userService.GetUserById(mediaItem.CreatorId);
                    if (creator == null)
                    {
                        break;
                    }

                    var mediaFilterSpecification = new MediaQuerySpecification(filter);
                    if (mediaFilterSpecification.IsSatisfiedBy(mediaItem))
                    {
                        result.Add(new MediaItemView(creator.Username, mediaItem));
                    }
                }
            }

            return result;
        }


        public FileContentResult GenerateCsv(IEnumerable<MediaItemView> results)
        {
            var csv = new StringBuilder();
            var headers = string.Join(",",
                "creator",
                "createdDate",
                "fileSize",
                "mediaType",
                "fileName",
                "imageWidth",
                "imageHeight",
                "isInBin",
                "BackofficeUrl",
                "url"
            );
            csv.AppendLine(headers);

            foreach (var mediaItem in results)
            {
                var row = string.Join(",",
                    mediaItem.Creator,
                    mediaItem.CreatedDate.ToString("G"),
                    mediaItem.FileSize,
                    mediaItem.MediaType,
                    mediaItem.FileName,
                    mediaItem.ImageWidth,
                    mediaItem.ImageHeight,
                    mediaItem.IsInBin,
                    mediaItem.BackofficeUrl,
                    mediaItem.Url
                );
                csv.AppendLine(row);
            }

            return new FileContentResult(Encoding.UTF8.GetBytes(csv.ToString()), MediaTypeNames.Application.Octet)
            {
                FileDownloadName = "UmbracoMediaReport.csv"
            };
        }
    }
}
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureStorageBlobs.Api
{
    public class UploadDigitalAssetCommand
    {
        public class Request : IRequest<Response> { }

        public class Response
        {
            public List<string> FileNames { get;set; }
        }

        public class Handler : IRequestHandler<Request, Response>
        {
            private CloudBlobClient _cloudBlobClient;
            private readonly CloudStorageAccount _cloudStorageAccount;
            private readonly IHttpContextAccessor _httpContextAccessor;
                        
            public Handler(
                CloudBlobClient cloudBlobClient,
                CloudStorageAccount cloudStorageAccount,
                IHttpContextAccessor httpContextAccessor) {
                _cloudBlobClient = cloudBlobClient;
                _cloudStorageAccount = cloudStorageAccount;
                _httpContextAccessor = httpContextAccessor;

                _cloudBlobClient = _cloudStorageAccount.CreateCloudBlobClient();
            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken) {

                var container = _cloudBlobClient.GetContainerReference("digitalAssets");

                await container.CreateIfNotExistsAsync();

                var httpContext = _httpContextAccessor.HttpContext;
                var defaultFormOptions = new FormOptions();
                
                if (!MultipartRequestHelper.IsMultipartContentType(httpContext.Request.ContentType))
                    throw new Exception($"Expected a multipart request, but got {httpContext.Request.ContentType}");

                var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(httpContext.Request.ContentType);

                var boundary = MultipartRequestHelper.GetBoundary(
                    mediaTypeHeaderValue,
                    defaultFormOptions.MultipartBoundaryLengthLimit);

                var reader = new MultipartReader(boundary, httpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();

                var fileNames = new List<string>();

                while (section != null)
                {
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {
                            using (var targetStream = new MemoryStream())
                            {
                                var fileName = $"{contentDisposition.FileName}".Trim(new char[] { '"' }).Replace("&", "and");
                                await section.Body.CopyToAsync(targetStream);
                                var blockBlob = container.GetBlockBlobReference(fileName);                                
                                await blockBlob.UploadFromStreamAsync(targetStream);
                                fileNames.Add(fileName);
                            }
                        }
                    }
                    
                    section = await reader.ReadNextSectionAsync();
                }
                
                return new Response()
                {
                    FileNames = fileNames
                };
            }
        }
    }
}

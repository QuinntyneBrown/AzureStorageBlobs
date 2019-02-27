using MediatR;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace AzureStorageBlobs.Api
{
    public class GetDigitalAssetByFileNameQuery
    {
        public class Request : IRequest<Response> {
            public string FileName { get; set; }
        }

        public class Response
        {

        }

        public class Handler : IRequestHandler<Request, Response>
        {            
            public Handler()
            {

            }

            public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}

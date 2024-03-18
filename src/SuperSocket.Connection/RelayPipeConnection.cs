using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using SuperSocket.ProtoBase;

namespace SuperSocket.Connection
{
    public class RelayPipeConnection<TPackageInfo> : PipeConnection<TPackageInfo>
        where TPackageInfo : class
    {
        static ConnectionOptions RebuildOptionsWithPipes(ConnectionOptions options, Pipe pipeIn, Pipe pipeOut)
        {
            options.Input = pipeIn;
            options.Output = pipeOut;
            return options;
        }

        public RelayPipeConnection(IPipelineFilter<TPackageInfo> pipelineFilter, ConnectionOptions options, Pipe pipeIn, Pipe pipeOut)
            : base(pipelineFilter, RebuildOptionsWithPipes(options, pipeIn, pipeOut))
        {

        }

        protected override void Close()
        {
            Input.Writer.Complete();
            Output.Writer.Complete();
        }

        protected override async ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            var writer = OutputWriter;
            var total = 0;

            foreach (var data in buffer)
            {
                var result = await writer.WriteAsync(data, cancellationToken);

                if (result.IsCompleted)
                    total += data.Length;
                else if (result.IsCanceled)
                    break;
            }

            return total;
        }

        protected override ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
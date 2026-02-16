using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Chunks
{
    public record ChunkDto(int Id, string Content);

    public interface IChunkQuery
    {
        Task<ChunkDto?> GetByIdAsync(int id, CancellationToken ct);
    }
}

using System;
using System.Text;

namespace Worker.Features.DocumentIntelligence;

public interface IDocumentTextract
{
    Task<StringBuilder> ExtractText(string documentName, CancellationToken cancellationToken);
}

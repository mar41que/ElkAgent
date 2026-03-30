using ElkAgent.Models;

namespace ElkAgent.Normalizers;

public interface INormalizer
{
    bool CanHandle(RawEvent rawEvent);
    EcsEvent Normalize(RawEvent rawEvent);
}
using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface IAppConfigService
    {
        WbsSettings WbsSettings { get; }
    }
}

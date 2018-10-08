using System.IO;
using System.Threading.Tasks;

namespace Install
{
    public interface IInstallationTokenProvider
    {
        Task<InstallationToken> GenerateAsync(InstallationTokenParameters input, StreamReader privateKeyReader);
    }
}

using System.IO;
using System.Threading.Tasks;

namespace Install
{
    public interface IInstallationTokenProvider
    {
        string GenerateJWT(InstallationTokenParameters input, string privateKey);

        Task<InstallationToken> GenerateAsync(InstallationTokenParameters input, string privateKey);
    }
}

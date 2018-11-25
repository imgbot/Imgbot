using System.IO;
using System.Threading.Tasks;

namespace Install
{
    public interface IInstallationTokenProvider
    {
        string GenerateJWT(InstallationTokenParameters input, StreamReader privateKeyReader);

        Task<InstallationToken> GenerateAsync(InstallationTokenParameters input, StreamReader privateKeyReader);
    }
}

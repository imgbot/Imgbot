using System.Threading.Tasks;

namespace ImgBot.Common.Mediation
{
    public interface IMediator
    {
        Task SendAsync<T>(T message);
    }
}

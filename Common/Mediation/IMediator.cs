using System.Threading.Tasks;

namespace Common.Mediation
{
    public interface IMediator
    {
        Task SendAsync<T>(T message);
    }
}

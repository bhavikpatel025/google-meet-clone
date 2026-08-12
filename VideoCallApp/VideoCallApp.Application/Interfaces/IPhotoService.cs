using System.IO;
using System.Threading.Tasks;

namespace VideoCallApp.Application.Interfaces;

public interface IPhotoService
{
    Task<string?> AddPhotoAsync(Stream fileStream, string fileName);
    Task<bool> DeletePhotoAsync(string publicUrl);
}

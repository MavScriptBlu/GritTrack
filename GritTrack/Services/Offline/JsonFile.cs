using System.Text.Json;

namespace GritTrack.Services.Offline
{
    // one small JSON file. a missing or broken file reads as "empty",
    // and writes go to a temp file first so a crash can't leave half a file
    public class JsonFile<T> where T : new()
    {
        private readonly string _path;

        public JsonFile(string path)
        {
            _path = path;
        }

        public async Task<T> ReadAsync()
        {
            try
            {
                if (!File.Exists(_path))
                    return new T();

                var json = await File.ReadAllTextAsync(_path);
                return JsonSerializer.Deserialize<T>(json) ?? new T();
            }
            catch (Exception exception) when (exception is IOException or JsonException)
            {
                return new T();
            }
        }

        public async Task WriteAsync(T value)
        {
            var tempPath = _path + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(value));
            File.Move(tempPath, _path, overwrite: true);
        }
    }
}

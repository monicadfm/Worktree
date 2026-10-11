namespace Worktree.Helpers
{
    public class ImageHelper : IImageHelper
    {
        public async Task<string> UploadImageAsync(IFormFile imageFile, string folder)
        {
            string guid = Guid.NewGuid().ToString();
            string file = $"{guid}{Path.GetExtension(imageFile.FileName).ToLower()}";

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, file);

            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/images/{folder}/{file}";
        }
    }
}
